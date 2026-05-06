using MediaDeck.Common.Base;
using MediaDeck.Core.Services.FileStatusUpdator;

namespace MediaDeck.ViewModels.Tools;

[Inject(InjectServiceLifetime.Singleton)]
public class BackgroundTasksViewModel : ViewModelBase {
	/// <summary>
	/// バックグラウンドタスク表示ViewModelを初期化する。
	/// </summary>
	/// <param name="fileStatusUpdater">ファイル状態更新サービス</param>
	/// <param name="updateFileHashBackgroundService">ファイルハッシュ更新サービス</param>
	public BackgroundTasksViewModel(FileStatusUpdatorService fileStatusUpdater, IFileHashUpdatorService updateFileHashBackgroundService) {
		this._fileStatusUpdater = fileStatusUpdater;
		this._updateFileHashBackgroundService = updateFileHashBackgroundService;
		this.FileStatusUpdaterTargetCount = this._fileStatusUpdater.TargetCount.ThrottleLast(TimeSpan.FromMilliseconds(100)).ObserveOnCurrentSynchronizationContext().ToBindableReactiveProperty().AddTo(this.CompositeDisposable);
		this.FileStatusUpdaterCompletedCount = this._fileStatusUpdater.CompletedCount.ThrottleLast(TimeSpan.FromMilliseconds(100)).ObserveOnCurrentSynchronizationContext().ToBindableReactiveProperty().AddTo(this.CompositeDisposable);
		this.UpdateFileHashBackgroundServiceTargetCount = this._updateFileHashBackgroundService.TargetCount.ThrottleLast(TimeSpan.FromMilliseconds(100)).ObserveOnCurrentSynchronizationContext().ToBindableReactiveProperty().AddTo(this.CompositeDisposable);
		this.UpdateFileHashBackgroundServiceCompletedCount = this._updateFileHashBackgroundService.CompletedCount.ThrottleLast(TimeSpan.FromMilliseconds(100)).ObserveOnCurrentSynchronizationContext().ToBindableReactiveProperty().AddTo(this.CompositeDisposable);
		this.FullHashUpdaterTargetCount = this._updateFileHashBackgroundService.FullHashTargetCount.ThrottleLast(TimeSpan.FromMilliseconds(100)).ObserveOnCurrentSynchronizationContext().ToBindableReactiveProperty().AddTo(this.CompositeDisposable);
		this.FullHashUpdaterCompletedCount = this._updateFileHashBackgroundService.FullHashCompletedCount.ThrottleLast(TimeSpan.FromMilliseconds(100)).ObserveOnCurrentSynchronizationContext().ToBindableReactiveProperty().AddTo(this.CompositeDisposable);

		this.TaskItems = [
			new BackgroundTaskStatusItemViewModel(
				"Update file status",
				this.FileStatusUpdaterCompletedCount,
				this.FileStatusUpdaterTargetCount,
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
				})
				.AddTo(this.CompositeDisposable),
			new BackgroundTaskStatusItemViewModel(
				"Update file hash",
				this.UpdateFileHashBackgroundServiceCompletedCount,
				this.UpdateFileHashBackgroundServiceTargetCount,
				() => this.Actions.OnNext(() => this._updateFileHashBackgroundService.EnqueueAllHashUpdatesAsync()),
				() => this._updateFileHashBackgroundService.CancelUpdate())
				.AddTo(this.CompositeDisposable),
			new BackgroundTaskStatusItemViewModel(
				"Update full hash",
				this.FullHashUpdaterCompletedCount,
				this.FullHashUpdaterTargetCount,
				() => this.Actions.OnNext(() => this._updateFileHashBackgroundService.CheckAndEnqueueFullHashUpdatesAsync()),
				() => this._updateFileHashBackgroundService.CancelFullHashUpdate())
				.AddTo(this.CompositeDisposable),
		];

		var runningStatusChanged = Observable.Merge(this.TaskItems.Select(x => x.IsRunning.Select(_ => Unit.Default)));
		this.RunningTaskItems = runningStatusChanged
			.Select(_ => this.GetRunningTaskItems())
			.ToBindableReactiveProperty(this.GetRunningTaskItems())
			.AddTo(this.CompositeDisposable);
		this.ActiveTaskCount = runningStatusChanged
			.Select(_ => this.TaskItems.Count(x => x.IsRunning.Value))
			.ToBindableReactiveProperty(this.TaskItems.Count(x => x.IsRunning.Value))
			.AddTo(this.CompositeDisposable);
		this.HasRunningTasks = this.ActiveTaskCount
			.Select(x => x > 0)
			.ToBindableReactiveProperty()
			.AddTo(this.CompositeDisposable);
		this.SummaryText = this.ActiveTaskCount
			.Select(count => count > 0 ? $"Background tasks running" : "Idle")
			.ToBindableReactiveProperty("Idle")
			.AddTo(this.CompositeDisposable);

		this.Actions.Synchronize()
			.ObserveOnThreadPool()
			.SubscribeAwait(async (action, ct) => await action().ConfigureAwait(false), AwaitOperation.Sequential, false)
			.AddTo(this.CompositeDisposable);
	}

	private readonly FileStatusUpdatorService _fileStatusUpdater;
	private readonly IFileHashUpdatorService _updateFileHashBackgroundService;
	private CancellationTokenSource _fileStatusUpdaterCts = new();

	/// <summary>
	/// ファイル状態更新の対象件数
	/// </summary>
	public BindableReactiveProperty<long> FileStatusUpdaterTargetCount {
		get;
	}

	/// <summary>
	/// ファイル状態更新の完了件数
	/// </summary>
	public BindableReactiveProperty<long> FileStatusUpdaterCompletedCount {
		get;
	}

	/// <summary>
	/// ファイルハッシュ更新の対象件数
	/// </summary>
	public BindableReactiveProperty<long> UpdateFileHashBackgroundServiceTargetCount {
		get;
	}

	/// <summary>
	/// ファイルハッシュ更新の完了件数
	/// </summary>
	public BindableReactiveProperty<long> UpdateFileHashBackgroundServiceCompletedCount {
		get;
	}

	/// <summary>
	/// フルハッシュ更新の対象件数
	/// </summary>
	public BindableReactiveProperty<long> FullHashUpdaterTargetCount {
		get;
	}

	/// <summary>
	/// フルハッシュ更新の完了件数
	/// </summary>
	public BindableReactiveProperty<long> FullHashUpdaterCompletedCount {
		get;
	}

	/// <summary>
	/// ステータス表示用タスク一覧
	/// </summary>
	public IReadOnlyList<BackgroundTaskStatusItemViewModel> TaskItems {
		get;
	}

	/// <summary>
	/// 実行中のタスク一覧
	/// </summary>
	public BindableReactiveProperty<IReadOnlyList<BackgroundTaskStatusItemViewModel>> RunningTaskItems {
		get;
	}

	/// <summary>
	/// 実行中タスク数
	/// </summary>
	public BindableReactiveProperty<int> ActiveTaskCount {
		get;
	}

	/// <summary>
	/// 実行中タスクがあるかどうか
	/// </summary>
	public BindableReactiveProperty<bool> HasRunningTasks {
		get;
	}

	/// <summary>
	/// ステータスバーに表示する概要テキスト
	/// </summary>
	public BindableReactiveProperty<string> SummaryText {
		get;
	}

	/// <summary>
	/// バックグラウンド実行キュー
	/// </summary>
	public Subject<Func<Task>> Actions {
		get;
	} = new();

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
	/// 実行中のタスク一覧を取得する。
	/// </summary>
	/// <returns>実行中タスク一覧</returns>
	private IReadOnlyList<BackgroundTaskStatusItemViewModel> GetRunningTaskItems() {
		return [.. this.TaskItems.Where(x => x.IsRunning.Value)];
	}

	/// <summary>
	/// リソースを解放する。
	/// </summary>
	/// <param name="disposing">マネージドリソースを解放するかどうか</param>
	protected override void Dispose(bool disposing) {
		if (disposing) {
			this._fileStatusUpdaterCts.Cancel();
			this._fileStatusUpdaterCts.Dispose();
		}
		base.Dispose(disposing);
	}
}