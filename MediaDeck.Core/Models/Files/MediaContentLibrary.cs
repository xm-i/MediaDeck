using System.Diagnostics;
using MediaDeck.Common.Base;
using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;
using MediaDeck.Composition.Interfaces.Notifications;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Core.Models.Files.Loaders;

namespace MediaDeck.Core.Models.Files;

[Inject(InjectServiceLifetime.Scoped)]
public class MediaContentLibrary : ModelBase {
	private readonly FilesLoader _filesLoader;
	private readonly SearchConfigModel _searchConfig;
	private readonly SearchConditionManager _searchConditionManager;

	/// <summary>コンストラクタ</summary>
	public MediaContentLibrary(FilesLoader filesLoader, SearchConfigModel searchConfig, SearchConditionManager searchConditionManager, ISearchConditionNotificationDispatcher dispatcher) {
		this._filesLoader = filesLoader;
		this._searchConfig = searchConfig;
		this._searchConditionManager = searchConditionManager;

		// Dispatcher の統合ストリームを監視する。
		// Switch により、新しい検索リクエストが来たら前の検索タスクを自動キャンセルする。
		dispatcher.SearchRequested
			.SubscribeAwait(async (_, ct) => {
				await Task.Run(async () => {
					await this.SearchAsync(ct).ConfigureAwait(false);
				}, ct).ConfigureAwait(false);
			}, AwaitOperation.Switch, false)
			.AddTo(this.CompositeDisposable);
	}

	/// <summary>検索結果ファイルリスト</summary>
	public ObservableList<IMediaItemModel> Files { get; } = [];

	/// <summary>現在の検索ワード（トークン）条件リスト</summary>
	public ObservableList<ISearchCondition> SearchConditions {
		get {
			return this._searchConditionManager.SearchConditions;
		}
	}

	/// <summary>検索条件候補リスト（サジェスト用）</summary>
	public ObservableList<ISearchCondition> SearchConditionCandidates {
		get {
			return this._searchConditionManager.SearchConditionCandidates;
		}
	}

	/// <summary>最後の検索にかかった時間（ミリ秒）</summary>
	public ReactiveProperty<long?> SearchElapsedMilliseconds { get; } = new();

	/// <summary>
	/// 検索を実行する。Switch により呼び出し元のキャンセルトークンが連携されるため、
	/// 古い検索タスクは自動的にキャンセルされる。
	/// </summary>
	private async ValueTask SearchAsync(CancellationToken token) {
		this.SearchElapsedMilliseconds.Value = null;
		var batch = new List<IMediaItemModel>();
		try {
			var stopwatch = Stopwatch.StartNew();

			var initialLoadCount = this._searchConfig.InitialLoadCount.Value;
			var incrementalLoadCount = this._searchConfig.IncrementalLoadCount.Value;
			var maxLoadCount = this._searchConfig.MaxLoadCount.Value;

			var stream = this._filesLoader.GetFilesStreamAsync(this.SearchConditions, token);

			var totalLoaded = 0;
			var isInitial = true;

			await foreach (var fileModel in stream.WithCancellation(token)) {
				batch.Add(fileModel.AddTo(this.CompositeDisposable));
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