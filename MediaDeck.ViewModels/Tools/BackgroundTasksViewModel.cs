using MediaDeck.Common.Base;
using MediaDeck.Core.Services.FileStatusUpdator;

namespace MediaDeck.ViewModels.Tools;

[Inject(InjectServiceLifetime.Singleton)]
public class BackgroundTasksViewModel : ViewModelBase {
	public BackgroundTasksViewModel(FileStatusUpdatorService fileStatusUpdater, IFileHashUpdatorService updateFileHashBackgroundService) {
		this._fileStatusUpdater = fileStatusUpdater;
		this._updateFileHashBackgroundService = updateFileHashBackgroundService;
		this.FileStatusUpdaterTargetCount = this._fileStatusUpdater.TargetCount.ThrottleLast(TimeSpan.FromMilliseconds(100)).ObserveOnCurrentSynchronizationContext().ToBindableReactiveProperty();
		this.FileStatusUpdaterCompletedCount = this._fileStatusUpdater.CompletedCount.ThrottleLast(TimeSpan.FromMilliseconds(100)).ObserveOnCurrentSynchronizationContext().ToBindableReactiveProperty();
		this.UpdateFileHashBackgroundServiceTargetCount = this._updateFileHashBackgroundService.TargetCount.ThrottleLast(TimeSpan.FromMilliseconds(100)).ObserveOnCurrentSynchronizationContext().ToBindableReactiveProperty();
		this.UpdateFileHashBackgroundServiceCompletedCount = this._updateFileHashBackgroundService.CompletedCount.ThrottleLast(TimeSpan.FromMilliseconds(100)).ObserveOnCurrentSynchronizationContext().ToBindableReactiveProperty();
		this.FullHashUpdaterTargetCount = this._updateFileHashBackgroundService.FullHashTargetCount.ThrottleLast(TimeSpan.FromMilliseconds(100)).ObserveOnCurrentSynchronizationContext().ToBindableReactiveProperty();
		this.FullHashUpdaterCompletedCount = this._updateFileHashBackgroundService.FullHashCompletedCount.ThrottleLast(TimeSpan.FromMilliseconds(100)).ObserveOnCurrentSynchronizationContext().ToBindableReactiveProperty();
		this.Actions.Synchronize().ObserveOnThreadPool().Subscribe(action => action());
	}

	private readonly FileStatusUpdatorService _fileStatusUpdater;
	private readonly IFileHashUpdatorService _updateFileHashBackgroundService;

	public BindableReactiveProperty<long> FileStatusUpdaterTargetCount {
		get;
	}

	public BindableReactiveProperty<long> FileStatusUpdaterCompletedCount {
		get;
	}

	public BindableReactiveProperty<long> UpdateFileHashBackgroundServiceTargetCount {
		get;
	}

	public BindableReactiveProperty<long> UpdateFileHashBackgroundServiceCompletedCount {
		get;
	}

	public BindableReactiveProperty<long> FullHashUpdaterTargetCount {
		get;
	}

	public BindableReactiveProperty<long> FullHashUpdaterCompletedCount {
		get;
	}

	public Subject<Action> Actions {
		get;
	} = new();

	public void Start() {
		this.Actions.OnNext(async () => await this._fileStatusUpdater.UpdateFileInfo());
	}
}