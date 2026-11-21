using MediaDeck.Common.Extensions;
using MediaDeck.Common.Utilities;
using MediaDeck.Composition.Database;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.Notifications;
using MediaDeck.Composition.Interfaces.Threading;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Core.Models.Files.SearchConditions;
using MediaDeck.Core.Models.NotificationDispatcher;
using MediaDeck.Core.Models.Repositories.Objects;
using MediaDeck.Core.Primitives;

namespace MediaDeck.Core.Models.Repositories;

/// <summary>
/// フォルダの階層構造および検索候補への反映を管理するリポジトリ。
/// </summary>
[Inject(InjectServiceLifetime.Scoped)]
public class FolderRepository : RepositoryBase {
	private readonly IDbContextFactory<MediaDeckDbContext> _dbFactory;
	private readonly ISearchConditionNotificationDispatcher _searchConditionNotificationDispatcher;
	private readonly TabStateModel _tabState;
	private readonly IUiDispatcher _uiDispatcher;
	private readonly AsyncLock _asyncLock = new();
	private ValueCountPair<string>[] _currentDirectoryStatusList = [];

	/// <summary>
	/// <see cref="FolderRepository"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="dbFactory">データベースコンテキストファクトリ。</param>
	/// <param name="searchConditionNotificationDispatcher">検索条件通知ディスパッチャ。</param>
	/// <param name="tabState">タブ状態モデル。</param>
	/// <param name="uiDispatcher">UI スレッドディスパッチャー。</param>
	public FolderRepository(
		IDbContextFactory<MediaDeckDbContext> dbFactory,
		ISearchConditionNotificationDispatcher searchConditionNotificationDispatcher,
		TabStateModel tabState,
		IUiDispatcher uiDispatcher) {
		this._dbFactory = dbFactory;
		this._searchConditionNotificationDispatcher = searchConditionNotificationDispatcher;
		this._tabState = tabState;
		this._uiDispatcher = uiDispatcher;
		this.RootFolder = new ReactiveProperty<FolderObject>().AddTo(this.CompositeDisposable);
		FileNotifications
			.FileRegistered
			.ThrottleLast(TimeSpan.FromSeconds(10))
			.SubscribeAwait(async (_, ct) => {
				try {
					await this.Load(ct).ConfigureAwait(false);
				} catch (OperationCanceledException) when (ct.IsCancellationRequested) {
				}
			}, AwaitOperation.Sequential, false)
			.AddTo(this.CompositeDisposable);
		FileNotifications
			.FilesChanged
			.ThrottleLast(TimeSpan.FromMilliseconds(500))
			.SubscribeAwait(async (_, ct) => {
				try {
					await this.Load(ct).ConfigureAwait(false);
				} catch (OperationCanceledException) when (ct.IsCancellationRequested) {
				}
			}, AwaitOperation.Sequential, false)
			.AddTo(this.CompositeDisposable);
	}

	/// <summary>
	/// ルートフォルダを取得します。
	/// </summary>
	public ReactiveProperty<FolderObject> RootFolder {
		get;
	}

	/// <summary>
	/// データベースからフォルダ階層をバックグラウンドで読み込み、UI スレッド上でツリーを反映します。
	/// </summary>
	/// <param name="cancellationToken">キャンセルトークン。</param>
	/// <returns>読み込み処理を表すタスク。</returns>
	public override async Task Load(CancellationToken cancellationToken = default) {
		if (this.DisposeState != DisposeState.NotDisposed) {
			return;
		}
		cancellationToken.ThrowIfCancellationRequested();

		using var _ = await this._asyncLock.LockAsync(cancellationToken).ConfigureAwait(false);
		if (this.DisposeState != DisposeState.NotDisposed) {
			return;
		}
		cancellationToken.ThrowIfCancellationRequested();

		var loadResult = await Task.Run(async () => {
			await using var db = await this._dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
			var list = (await db
					.MediaItems
					.GroupBy(x => x.DirectoryPath)
					.Select(x => new ValueCountPair<string>(x.Key, x.Count()))
					.ToListAsync(cancellationToken)
					.ConfigureAwait(false))
				.OrderBy(x => x.Value)
				.ToList();

			var directoryStatusList = list.ToArray();
			if (directoryStatusList.SequenceEqual(this._currentDirectoryStatusList)) {
				return new FolderLoadResult(false, directoryStatusList, null);
			}

			var rootFolder = BuildFolderTree(list, cancellationToken);
			return new FolderLoadResult(true, directoryStatusList, rootFolder);
		}, cancellationToken).ConfigureAwait(false);

		if (!loadResult.Changed) {
			if (this.RootFolder.Value != null) {
				return;
			}
			if (this.DisposeState != DisposeState.NotDisposed) {
				return;
			}
			cancellationToken.ThrowIfCancellationRequested();

			var emptyRoot = new FolderObject(null, "", []);
			await this._uiDispatcher.RunAsync(() => {
				if (this.DisposeState != DisposeState.NotDisposed || cancellationToken.IsCancellationRequested) {
					return;
				}
				this.RootFolder.Value ??= emptyRoot;
			}).ConfigureAwait(false);
			cancellationToken.ThrowIfCancellationRequested();
			return;
		}

		if (this.DisposeState != DisposeState.NotDisposed) {
			return;
		}
		cancellationToken.ThrowIfCancellationRequested();

		var newRoot = loadResult.RootFolder!;
		await this._uiDispatcher.RunAsync(() => {
			if (this.DisposeState != DisposeState.NotDisposed || cancellationToken.IsCancellationRequested) {
				return;
			}
			this.Restore(newRoot);
			this._currentDirectoryStatusList = loadResult.StatusList;
			this.RootFolder.Value = newRoot;
		}).ConfigureAwait(false);
		cancellationToken.ThrowIfCancellationRequested();
	}

	/// <summary>
	/// 集計済みのフォルダ一覧から中間分岐ノードを補完してルートフォルダを構築します。
	/// </summary>
	/// <param name="list">ディレクトリパスとファイル件数のペア一覧。</param>
	/// <param name="cancellationToken">キャンセルトークン。</param>
	/// <returns>構築されたルートフォルダオブジェクト。</returns>
	private static FolderObject BuildFolderTree(List<ValueCountPair<string>> list, CancellationToken cancellationToken) {
		var all = list.Select(x => (x.Value, x.Count, Split: x.Value.Split(Path.DirectorySeparatorChar))).ToArray();
		var maxPathDepth = all.Length == 0 ? 0 : all.Max(x => x.Split.Length);

		// ルート追加
		list.Add(new("", list.Sum(x => x.Count)));

		// 足りない部分を補足
		for (var depth = 1; depth <= maxPathDepth; depth++) {
			cancellationToken.ThrowIfCancellationRequested();

			// 最大の深さまで
			var target = all.Where(x => x.Split.Length > depth).ToArray(); // 深さが足りないものを除外
			list.AddRange(target
				.Select(x =>
				(
					item: x,
					joined: string.Join(Path.DirectorySeparatorChar, x.Split[0..depth]), // 対象の深さまでのパス
					joinedPlus: string.Join(Path.DirectorySeparatorChar, x.Split[0..(depth + 1)]) // 対象の深さ+1までのパス
				))
				.Where(x => !all.Any(y => y.Value == x.joined)) // 対象のパスがすでに含まれていたら除外
				.GroupBy(x => x.joined) // 対象の深さまでのパスでグループ化
				.Where(x => x.DistinctBy(x => x.joinedPlus).Count() >= 2) // 対象の深さから、2つ以上のフォルダに分岐している
				.Select(x => new ValueCountPair<string>(x.Key, 0)));
		}

		return new FolderObject(null, "", [.. list.OrderBy(x => x.Value)]);
	}

	/// <summary>
	/// 選択されたフォルダを検索条件として設定します。
	/// </summary>
	/// <param name="folderObject">対象フォルダ。</param>
	/// <param name="includeSubDirectory">サブディレクトリを含めるかどうか。</param>
	public void SetRepositoryCandidate(FolderObject folderObject, bool includeSubDirectory) {
		this._searchConditionNotificationDispatcher.UpdateRequest.OnNext(x => {
			x.RemoveRange(x.Where(x => x is IRepositorySearchCondition));
			x.Add(new FolderSearchCondition { FolderPath = folderObject.FolderPath, IncludeSubDirectories = includeSubDirectory });
		});
	}

	/// <summary>
	/// すべてのフォルダを取得します。
	/// </summary>
	/// <returns>フォルダオブジェクトの列挙子。ルートフォルダが未ロードの場合は空の列挙子を返します。</returns>
	public IEnumerable<FolderObject> GetAllFolders() {
		var root = this.RootFolder.Value;
		if (root == null) {
			return [];
		}
		IEnumerable<FolderObject> func(FolderObject fo) => fo.ChildFolders.SelectMany(x => func(x)).Concat([fo]).OrderBy(x => x.FolderPath);
		return func(root);
	}

	/// <summary>
	/// タブ状態に保存されたフォルダ検索条件に従ってツリーの展開状態を復元します。
	/// </summary>
	/// <param name="rootFolder">展開状態を復元するルートフォルダ。</param>
	private void Restore(FolderObject rootFolder) {
		var condition = this._tabState.SearchState.SearchCondition.OfType<FolderSearchCondition>().FirstOrDefault();
		if (condition == null) {
			return;
		}
		var parent = rootFolder;
		while (true) {
			var previousParent = parent;
			foreach (var child in parent.ChildFolders) {
				if (condition.FolderPath.StartsWith(child.FolderPath)) {
					child.IsExpanded = true;
					parent = child;
					break;
				}
			}
			if (previousParent == parent) {
				break;
			}
		}
	}

	/// <summary>
	/// バックグラウンド読み込み処理の結果を保持します。
	/// </summary>
	/// <param name="Changed">前回状態から変更があったかどうか。</param>
	/// <param name="StatusList">最新のディレクトリ状態一覧。</param>
	/// <param name="RootFolder">構築されたルートフォルダ（変更なし時は <c>null</c>）。</param>
	private readonly record struct FolderLoadResult(bool Changed, ValueCountPair<string>[] StatusList, FolderObject? RootFolder);
}