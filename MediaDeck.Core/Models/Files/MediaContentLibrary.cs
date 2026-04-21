using System.Diagnostics;

using MediaDeck.Common.Base;
using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Composition.Interfaces.FileTypes.Models;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Core.Models.Files.Loaders;
using MediaDeck.Core.Models.Files.SearchConditions;
using MediaDeck.Core.Models.NotificationDispatcher;
using MediaDeck.Core.Models.Repositories;

namespace MediaDeck.Core.Models.Files;

[Inject(InjectServiceLifetime.Scoped)]
public class MediaContentLibrary : ModelBase {
	/// <summary>コンストラクタ</summary>
	public MediaContentLibrary(FilesLoader filesLoader, SearchConfigModel searchConfig, SearchConditionNotificationDispatcher dispatcher, ITagsManager tagsManager, FolderRepository folderRepository, TabStateModel tabState) {
		this._filesLoader = filesLoader;
		this._searchConfig = searchConfig;

		// 検索ワード（トークン）の追加・削除・更新を SearchConditions リストに反映する
		dispatcher.AddRequest.Subscribe(this.SearchConditions.Add).AddTo(this.CompositeDisposable);
		dispatcher.RemoveRequest.Subscribe(x => this.SearchConditions.Remove(x)).AddTo(this.CompositeDisposable);
		dispatcher.UpdateRequest.Subscribe(x => x(this.SearchConditions)).AddTo(this.CompositeDisposable);

		// タブ状態の初期値を SearchConditions に復元する
		this.SearchConditions.AddRange(tabState.SearchState.SearchCondition.ToArray());

		// SearchConditions の変更をタブ状態に同期する
		this.SearchConditions.ObserveChanged()
			.Subscribe(_ => {
				tabState.SearchState.SearchCondition.Clear();
				tabState.SearchState.SearchCondition.AddRange(this.SearchConditions.ToArray());
			})
			.AddTo(this.CompositeDisposable);

		// 候補リストを初期化する
		this.SearchConditionCandidates.AddRange(tagsManager.Tags.Select(x => new TagSearchCondition(tagsManager) { TagId = x.TagId } as ISearchCondition));
		this.SearchConditionCandidates.AddRange(folderRepository.GetAllFolders().Select(x => new FolderSearchCondition { FolderPath = x.FolderPath } as ISearchCondition));

		// Dispatcher の統合ストリームを監視する。
		// Switch により、新しい検索リクエストが来たら前の検索タスクを自動キャンセルする。
		dispatcher.SearchRequested
			.ObserveOnThreadPool()
			.SubscribeAwait(async (_, cts) => await this.SearchAsync(cts).ConfigureAwait(false), AwaitOperation.Drop, false)
			.AddTo(this.CompositeDisposable);
	}

	private readonly FilesLoader _filesLoader;
	private readonly SearchConfigModel _searchConfig;

	/// <summary>検索結果ファイルリスト</summary>
	public ObservableList<IFileModel> Files { get; } = [];

	/// <summary>現在の検索ワード（トークン）条件リスト</summary>
	public ObservableList<ISearchCondition> SearchConditions { get; } = [];

	/// <summary>検索条件候補リスト（サジェスト用）</summary>
	public ObservableList<ISearchCondition> SearchConditionCandidates { get; } = [];

	/// <summary>最後の検索にかかった時間（ミリ秒）</summary>
	public ReactiveProperty<long?> SearchElapsedMilliseconds { get; } = new();

	/// <summary>
	/// 検索を実行する。Switch により呼び出し元のキャンセルトークンが連携されるため、
	/// 古い検索タスクは自動的にキャンセルされる。
	/// </summary>
	private async ValueTask SearchAsync(CancellationToken token) {
		this.SearchElapsedMilliseconds.Value = null;
		var batch = new List<IFileModel>();
		try {
			var stopwatch = Stopwatch.StartNew();

			var initialLoadCount = this._searchConfig.InitialLoadCount.Value;
			var incrementalLoadCount = this._searchConfig.IncrementalLoadCount.Value;
			var maxLoadCount = this._searchConfig.MaxLoadCount.Value;

			var stream = this._filesLoader.GetFilesStreamAsync(this.SearchConditions, token);

			var totalLoaded = 0;
			var isInitial = true;

			await foreach (var fileModel in stream.WithCancellation(token)) {
				batch.Add(fileModel);
				totalLoaded++;

				if (isInitial && batch.Count >= initialLoadCount) {
					this.ClearFiles();
					this.Files.AddRange(batch);
					batch.Clear();
					isInitial = false;
				} else if (!isInitial && batch.Count >= incrementalLoadCount) {
					this.Files.AddRange(batch);
					batch.Clear();
				}

				if (totalLoaded >= maxLoadCount) {
					break;
				}
			}

			if (batch.Count > 0) {
				if (isInitial) {
					this.ClearFiles();
				}
				this.Files.AddRange(batch);
				batch.Clear();
			} else if (isInitial && totalLoaded == 0) {
				this.ClearFiles();
			}

			stopwatch.Stop();
			this.SearchElapsedMilliseconds.Value = stopwatch.ElapsedMilliseconds;
		} catch (OperationCanceledException) when (token.IsCancellationRequested) {
			// 新しい検索によりキャンセルされた場合は何もしない
		} finally {
			foreach (var file in batch) {
				file.Dispose();
			}
		}
	}

	/// <summary>ファイルリストをクリアし、各要素を Dispose する。</summary>
	private void ClearFiles() {
		foreach (var file in this.Files) {
			file.Dispose();
		}
		this.Files.Clear();
	}
}