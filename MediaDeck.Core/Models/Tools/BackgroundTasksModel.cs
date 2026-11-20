using MediaDeck.Common.Base;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Core.Services.FileStatusUpdator;
using MediaDeck.Core.Services.MediaItemMetadataUpdator;

namespace MediaDeck.Core.Models.Tools;

/// <summary>
/// バックグラウンドタスク管理Model。
/// </summary>
[Inject(InjectServiceLifetime.Singleton)]
public class BackgroundTasksModel : ModelBase {
	private readonly FileStatusUpdatorService _fileStatusUpdater;
	private readonly IFileHashUpdatorService _updateFileHashBackgroundService;
	private readonly MediaItemMetadataUpdatorService _metadataUpdator;
	private readonly IStringProvider _stringProvider;
	private CancellationTokenSource _fileStatusUpdaterCts = new();
	private CancellationTokenSource _metadataUpdatorCts = new();

	/// <summary>
	/// バックグラウンド実行キュー
	/// </summary>
	public Subject<Func<Task>> Actions {
		get;
	} = new();

	/// <summary>
	/// バックグラウンドタスク項目一覧
	/// </summary>
	public IReadOnlyList<BackgroundTaskStatusItemModel> TaskItems {
		get;
	}

	/// <summary>
	/// バックグラウンドタスク管理Modelを初期化する。
	/// </summary>
	/// <param name="fileStatusUpdater">ファイル状態更新サービス</param>
	/// <param name="updateFileHashBackgroundService">ファイルハッシュ更新サービス</param>
	public BackgroundTasksModel(FileStatusUpdatorService fileStatusUpdater, IFileHashUpdatorService updateFileHashBackgroundService, MediaItemMetadataUpdatorService metadataUpdator, IStringProvider stringProvider) {
		this._fileStatusUpdater = fileStatusUpdater;
		this._updateFileHashBackgroundService = updateFileHashBackgroundService;
		this._metadataUpdator = metadataUpdator;
		this._stringProvider = stringProvider;

		this.TaskItems = [
			new BackgroundTaskStatusItemModel(
				this._stringProvider.GetString("BackgroundTask_UpdateFileStatus"),
				this._fileStatusUpdater.CompletedCount,
				this._fileStatusUpdater.TargetCount,
				() => {
					this._fileStatusUpdaterCts.Cancel();
					this._fileStatusUpdaterCts.Dispose();
					this._fileStatusUpdaterCts = new();
					this.Actions.OnNext(() => this._fileStatusUpdater.UpdateFileInfo(this._fileStatusUpdaterCts.Token));
				},
				() => {
					this._fileStatusUpdaterCts.Cancel();
					this._fileStatusUpdater.TargetCount.Value = 0;
					this._fileStatusUpdater.CompletedCount.Value = 0;
				}),
			new BackgroundTaskStatusItemModel(
				this._stringProvider.GetString("BackgroundTask_UpdateFileHash"),
				this._updateFileHashBackgroundService.CompletedCount,
				this._updateFileHashBackgroundService.TargetCount,
				() => this.Actions.OnNext(() => this._updateFileHashBackgroundService.EnqueueAllHashUpdatesAsync(CancellationToken.None)),
				() => this._updateFileHashBackgroundService.CancelUpdate()),
			new BackgroundTaskStatusItemModel(
				this._stringProvider.GetString("BackgroundTask_UpdateFullHash"),
				this._updateFileHashBackgroundService.FullHashCompletedCount,
				this._updateFileHashBackgroundService.FullHashTargetCount,
				() => this.Actions.OnNext(() => this._updateFileHashBackgroundService.CheckAndEnqueueFullHashUpdatesAsync(CancellationToken.None)),
				() => this._updateFileHashBackgroundService.CancelFullHashUpdate()),
			new BackgroundTaskStatusItemModel(
				this._stringProvider.GetString("BackgroundTask_UpdateMetadata"),
				this._metadataUpdator.CompletedCount,
				this._metadataUpdator.TargetCount,
				() => {
					this._metadataUpdatorCts.Cancel();
					this._metadataUpdatorCts.Dispose();
					this._metadataUpdatorCts = new();
					this.Actions.OnNext(() => this._metadataUpdator.UpdateMetadataAsync(this._metadataUpdatorCts.Token));
				},
				() => {
					this._metadataUpdatorCts.Cancel();
					this._metadataUpdator.TargetCount.Value = 0;
					this._metadataUpdator.CompletedCount.Value = 0;
				}),
		];

		this.Actions.Synchronize()
			.ObserveOnThreadPool()
			.SubscribeAwait(async (action, ct) => await action().ConfigureAwait(false), AwaitOperation.Sequential, false)
			.AddTo(this.CompositeDisposable);
	}

	/// <summary>
	/// バックグラウンドタスクを開始する。
	/// </summary>
	public void Start() {
		this._fileStatusUpdaterCts.Cancel();
		this._fileStatusUpdaterCts.Dispose();
		this._fileStatusUpdaterCts = new();
		this.Actions.OnNext(() => this._fileStatusUpdater.UpdateFileInfo(this._fileStatusUpdaterCts.Token));
	}

	/// <summary>
	/// メタデータの更新をキューに追加する。
	/// </summary>
	/// <param name="ids">更新対象のメディアアイテムID</param>
	public void EnqueueMetadataUpdate(IEnumerable<long> ids) {
		if (this._metadataUpdatorCts.IsCancellationRequested) {
			this._metadataUpdatorCts.Dispose();
			this._metadataUpdatorCts = new();
		}
		this.Actions.OnNext(() => this._metadataUpdator.UpdateMetadataAsync(ids, this._metadataUpdatorCts.Token));
	}

	/// <summary>
	/// リソースを解放する。
	/// </summary>
	/// <param name="disposing">マネージドリソースを解放するかどうか</param>
	protected override void Dispose(bool disposing) {
		if (disposing) {
			this._fileStatusUpdaterCts.Cancel();
			this._fileStatusUpdaterCts.Dispose();
			this._metadataUpdatorCts.Cancel();
			this._metadataUpdatorCts.Dispose();
		}
		base.Dispose(disposing);
	}
}