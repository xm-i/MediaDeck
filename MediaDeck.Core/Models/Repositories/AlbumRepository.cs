using MediaDeck.Common.Extensions;
using MediaDeck.Common.Utilities;
using MediaDeck.Composition.Database;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.Notifications;
using MediaDeck.Composition.Interfaces.Threading;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Composition.Tables;
using MediaDeck.Core.Models.Files.SearchConditions;
using MediaDeck.Core.Models.NotificationDispatcher;
using MediaDeck.Core.Models.Repositories.Objects;
using MediaDeck.Core.Primitives;

namespace MediaDeck.Core.Models.Repositories;

/// <summary>
/// アルバムの階層構造および検索候補への反映を管理するリポジトリ。
/// </summary>
[Inject(InjectServiceLifetime.Scoped)]
public class AlbumRepository : RepositoryBase {
	private readonly IDbContextFactory<MediaDeckDbContext> _dbFactory;
	private readonly ISearchConditionNotificationDispatcher _searchConditionNotificationDispatcher;
	private readonly TabStateModel _tabState;
	private readonly IUiDispatcher _uiDispatcher;
	private readonly AsyncLock _asyncLock = new();
	private ValueCountPair<string>[] _currentAlbumStatusList = [];

	/// <summary>
	/// <see cref="AlbumRepository"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="dbFactory">データベースコンテキストファクトリ。</param>
	/// <param name="searchConditionNotificationDispatcher">検索条件通知ディスパッチャ。</param>
	/// <param name="tabState">タブ状態モデル。</param>
	/// <param name="uiDispatcher">UI スレッドディスパッチャー。</param>
	public AlbumRepository(
		IDbContextFactory<MediaDeckDbContext> dbFactory,
		ISearchConditionNotificationDispatcher searchConditionNotificationDispatcher,
		TabStateModel tabState,
		IUiDispatcher uiDispatcher) {
		this._dbFactory = dbFactory;
		this._searchConditionNotificationDispatcher = searchConditionNotificationDispatcher;
		this._tabState = tabState;
		this._uiDispatcher = uiDispatcher;
		this.RootAlbum = new ReactiveProperty<AlbumObject>().AddTo(this.CompositeDisposable);
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
		AlbumNotifications
			.AlbumUpdated
			.Where(sender => !ReferenceEquals(sender, this))
			.SubscribeAwait(async (_, ct) => {
				try {
					await this.Load(ct).ConfigureAwait(false);
				} catch (OperationCanceledException) when (ct.IsCancellationRequested) {
				}
			}, AwaitOperation.Sequential, false)
			.AddTo(this.CompositeDisposable);
	}

	/// <summary>
	/// ルートアルバムを取得します。
	/// </summary>
	public ReactiveProperty<AlbumObject> RootAlbum {
		get;
	}

	/// <summary>
	/// データベースからアルバム階層をバックグラウンドで読み込み、UI スレッド上でツリーを反映します。
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

			// 全アルバムと、それぞれの直接登録メディア件数
			var counts = await db.Albums
				.Select(a => new ValueCountPair<string>(a.Path, a.MediaItemAlbums.Count()))
				.ToListAsync(cancellationToken)
				.ConfigureAwait(false);

			var list = counts
				.OrderBy(x => x.Value)
				.ToList();

			var statusArray = list.ToArray();
			if (statusArray.SequenceEqual(this._currentAlbumStatusList)) {
				return new AlbumLoadResult(false, statusArray, null);
			}

			var rootAlbum = BuildAlbumTree(list, cancellationToken);
			return new AlbumLoadResult(true, statusArray, rootAlbum);
		}, cancellationToken).ConfigureAwait(false);

		if (!loadResult.Changed) {
			if (this.RootAlbum.Value != null) {
				return;
			}
			if (this.DisposeState != DisposeState.NotDisposed) {
				return;
			}
			cancellationToken.ThrowIfCancellationRequested();

			var emptyRoot = new AlbumObject(null, "", []);
			await this._uiDispatcher.RunAsync(() => {
				if (this.DisposeState != DisposeState.NotDisposed || cancellationToken.IsCancellationRequested) {
					return;
				}
				this.RootAlbum.Value ??= emptyRoot;
			}).ConfigureAwait(false);
			cancellationToken.ThrowIfCancellationRequested();
			return;
		}

		if (this.DisposeState != DisposeState.NotDisposed) {
			return;
		}
		cancellationToken.ThrowIfCancellationRequested();

		var newRoot = loadResult.RootAlbum!;
		await this._uiDispatcher.RunAsync(() => {
			if (this.DisposeState != DisposeState.NotDisposed || cancellationToken.IsCancellationRequested) {
				return;
			}
			this.Restore(newRoot);
			this._currentAlbumStatusList = loadResult.StatusList;
			this.RootAlbum.Value = newRoot;
		}).ConfigureAwait(false);
		cancellationToken.ThrowIfCancellationRequested();
	}

	/// <summary>
	/// 集計済みのアルバム一覧から中間分岐ノードを補完してルートアルバムを構築します。
	/// </summary>
	/// <param name="list">アルバムパスと登録件数のペア一覧。</param>
	/// <param name="cancellationToken">キャンセルトークン。</param>
	/// <returns>構築されたルートアルバムオブジェクト。</returns>
	private static AlbumObject BuildAlbumTree(List<ValueCountPair<string>> list, CancellationToken cancellationToken) {
		var all = list.Select(x => (x.Value, x.Count, Split: x.Value.Split(AlbumObject.Separator))).ToArray();
		var maxPathDepth = all.Length == 0 ? 0 : all.Max(x => x.Split.Length);

		// ルート
		list.Add(new("", list.Sum(x => x.Count)));

		// 中間ノード補完
		for (var depth = 1; depth <= maxPathDepth; depth++) {
			cancellationToken.ThrowIfCancellationRequested();

			var target = all.Where(x => x.Split.Length > depth).ToArray();
			list.AddRange(target
				.Select(x => (
					item: x,
					joined: string.Join(AlbumObject.Separator, x.Split[0..depth]),
					joinedPlus: string.Join(AlbumObject.Separator, x.Split[0..(depth + 1)])))
				.Where(x => !all.Any(y => y.Value == x.joined))
				.GroupBy(x => x.joined)
				.Where(x => x.DistinctBy(x => x.joinedPlus).Count() >= 2)
				.Select(x => new ValueCountPair<string>(x.Key, 0)));
		}

		return new AlbumObject(null, "", [.. list.OrderBy(x => x.Value)]);
	}

	/// <summary>
	/// 選択されたアルバムを検索条件として設定します。
	/// </summary>
	/// <param name="albumObject">対象アルバム。</param>
	/// <param name="includeSubAlbums">サブアルバムを含めるかどうか。</param>
	public void SetRepositoryCandidate(AlbumObject albumObject, bool includeSubAlbums) {
		this._searchConditionNotificationDispatcher.UpdateRequest.OnNext(x => {
			x.RemoveRange(x.Where(c => c is IRepositorySearchCondition));
			x.Add(new AlbumSearchCondition { AlbumPath = albumObject.AlbumPath, IncludeSubAlbums = includeSubAlbums });
		});
	}

	/// <summary>
	/// 指定 Path のアルバムを取得。存在しない場合は新規作成。
	/// </summary>
	/// <param name="path">アルバムパス。</param>
	/// <returns>取得または作成された <see cref="Album"/>。</returns>
	public async Task<Album> GetOrCreateAsync(string path) {
		path = NormalizePath(path);
		await using var db = await this._dbFactory.CreateDbContextAsync();
		var existing = await db.Albums.FirstOrDefaultAsync(a => a.Path == path);
		if (existing != null) {
			return existing;
		}
		var album = new Album { Path = path };
		db.Albums.Add(album);
		await db.SaveChangesAsync();
		await this.Load();
		AlbumNotifications.AlbumUpdated.OnNext(this);
		return album;
	}

	/// <summary>
	/// 指定 Path の単アルバムを削除します。配下にサブアルバムが存在する場合は削除しません。
	/// </summary>
	/// <param name="path">削除するアルバムのパス。</param>
	/// <returns>削除できた場合は <c>true</c>、対象が存在しないか子アルバムがあるため削除できなかった場合は <c>false</c>。</returns>
	public async Task<bool> DeleteAlbumAsync(string path) {
		path = NormalizePath(path);
		var prefix = $"{path}{AlbumObject.Separator}";
		await using var db = await this._dbFactory.CreateDbContextAsync();

		if (await db.Albums.AnyAsync(a => a.Path.StartsWith(prefix))) {
			await this.Load();
			return false;
		}

		var album = await db.Albums
			.Include(a => a.MediaItemAlbums)
			.FirstOrDefaultAsync(a => a.Path == path);
		if (album == null) {
			await this.Load();
			return false;
		}

		db.Albums.Remove(album);
		await db.SaveChangesAsync();
		await this.Load();
		AlbumNotifications.AlbumUpdated.OnNext(this);
		return true;
	}

	/// <summary>
	/// 指定 Path のアルバム（配下のサブアルバムを含む）を新しい Path にリネームします。
	/// </summary>
	/// <param name="oldPath">変更前のアルバムパス。</param>
	/// <param name="newPath">変更後のアルバムパス。</param>
	/// <returns>リネーム処理の結果。</returns>
	public async Task<AlbumRenameResult> RenameAlbumAsync(string oldPath, string newPath) {
		oldPath = NormalizePath(oldPath);
		newPath = NormalizePath(newPath);

		if (string.Equals(oldPath, newPath, StringComparison.Ordinal)) {
			return AlbumRenameResult.Success;
		}

		var oldPrefix = $"{oldPath}{AlbumObject.Separator}";
		if (newPath.StartsWith(oldPrefix, StringComparison.OrdinalIgnoreCase)) {
			return AlbumRenameResult.InvalidDestinationUnderSource;
		}

		await using var db = await this._dbFactory.CreateDbContextAsync();
		var allAlbums = await db.Albums.ToListAsync();

		var targets = allAlbums
			.Where(a => string.Equals(a.Path, oldPath, StringComparison.OrdinalIgnoreCase)
				|| a.Path.StartsWith(oldPrefix, StringComparison.OrdinalIgnoreCase))
			.ToList();

		if (targets.Count == 0) {
			await this.Load();
			return AlbumRenameResult.NotFound;
		}

		var targetSet = targets.ToHashSet();
		var otherPaths = allAlbums
			.Where(a => !targetSet.Contains(a))
			.Select(a => a.Path)
			.ToHashSet(StringComparer.OrdinalIgnoreCase);

		var newPrefix = $"{newPath}{AlbumObject.Separator}";
		if (!string.Equals(oldPath, newPath, StringComparison.OrdinalIgnoreCase)) {
			if (otherPaths.Contains(newPath)) {
				return AlbumRenameResult.DuplicatePath;
			}

			var newDirectChildrenCount = otherPaths
				.Where(p => p.StartsWith(newPrefix, StringComparison.OrdinalIgnoreCase))
				.Select(p => {
					var rest = p[newPrefix.Length..];
					var slashIndex = rest.IndexOf(AlbumObject.Separator);
					return slashIndex >= 0 ? rest[..slashIndex] : rest;
				})
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.Count();
			if (newDirectChildrenCount >= 2) {
				return AlbumRenameResult.DuplicatePath;
			}
		}

		var updates = new List<(Album Album, string UpdatedPath)>(targets.Count);
		foreach (var album in targets) {
			var updatedPath = string.Equals(album.Path, oldPath, StringComparison.OrdinalIgnoreCase)
				? newPath
				: $"{newPath}{album.Path[oldPath.Length..]}";
			if (otherPaths.Contains(updatedPath)) {
				return AlbumRenameResult.DuplicatePath;
			}
			updates.Add((album, updatedPath));
		}

		foreach (var (album, updatedPath) in updates) {
			album.Path = updatedPath;
		}

		await db.SaveChangesAsync();
		await this.Load();
		AlbumNotifications.AlbumUpdated.OnNext(this);
		return AlbumRenameResult.Success;
	}

	/// <summary>
	/// 指定したメディアアイテム群を、指定 Path のアルバムへ追加する。
	/// アルバムが存在しなければ新規作成する。
	/// </summary>
	/// <param name="albumPath">アルバムパス。</param>
	/// <param name="mediaItemIds">追加するメディアアイテム ID 一覧。</param>
	public async Task AddItemsAsync(string albumPath, IEnumerable<long> mediaItemIds) {
		var path = NormalizePath(albumPath);
		await using var db = await this._dbFactory.CreateDbContextAsync();
		var album = await db.Albums.FirstOrDefaultAsync(a => a.Path == path);
		if (album == null) {
			album = new Album { Path = path };
			db.Albums.Add(album);
			await db.SaveChangesAsync();
		}
		album.LastAccessedTime = DateTime.UtcNow;

		var ids = mediaItemIds.Distinct().ToArray();
		var existingPairs = await db.MediaItemAlbums
			.Where(mia => mia.AlbumId == album.AlbumId && ids.Contains(mia.MediaItemId))
			.Select(mia => mia.MediaItemId)
			.ToListAsync();

		foreach (var id in ids.Except(existingPairs)) {
			db.MediaItemAlbums.Add(new MediaItemAlbum {
				AlbumId = album.AlbumId,
				MediaItemId = id,
			});
		}
		await db.SaveChangesAsync();
		await this.Load();
		AlbumNotifications.AlbumUpdated.OnNext(this);
	}

	/// <summary>
	/// 全アルバムを最近使用順で取得。
	/// </summary>
	public async Task<IReadOnlyList<Album>> GetRecentAlbumsAsync(int max = 20) {
		await using var db = await this._dbFactory.CreateDbContextAsync();
		return await db.Albums
			.AsNoTracking()
			.OrderByDescending(a => a.LastAccessedTime)
			.Take(max)
			.ToListAsync();
	}

	private static string NormalizePath(string path) {
		path = (path ?? "").Trim().Replace('\\', AlbumObject.Separator);
		path = path.Trim(AlbumObject.Separator);
		if (string.IsNullOrEmpty(path)) {
			throw new ArgumentException("Album path must not be empty.");
		}
		return path;
	}

	/// <summary>
	/// タブ状態に保存されたアルバム検索条件に従ってツリーの展開状態を復元します。
	/// </summary>
	/// <param name="rootAlbum">展開状態を復元するルートアルバム。</param>
	private void Restore(AlbumObject rootAlbum) {
		var condition = this._tabState.SearchState.SearchCondition.OfType<AlbumSearchCondition>().FirstOrDefault();
		if (condition == null) {
			return;
		}
		var parent = rootAlbum;
		while (true) {
			var previousParent = parent;
			foreach (var child in parent.ChildAlbums) {
				if (condition.AlbumPath.StartsWith(child.AlbumPath)) {
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
	/// <param name="StatusList">最新のアルバム状態一覧。</param>
	/// <param name="RootAlbum">構築されたルートアルバム（変更なし時は <c>null</c>）。</param>
	private readonly record struct AlbumLoadResult(bool Changed, ValueCountPair<string>[] StatusList, AlbumObject? RootAlbum);
}