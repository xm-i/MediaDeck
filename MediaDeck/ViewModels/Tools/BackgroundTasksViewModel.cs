using System.Threading.Tasks;

using MediaDeck.Composition.Bases;
using MediaDeck.Models.Tools;

namespace MediaDeck.ViewModels.Tools;
[Inject(InjectServiceLifetime.Singleton)]
public class BackgroundTasksViewModel: ViewModelBase {

	public BackgroundTasksViewModel(FileStatusUpdater fileStatusUpdater, FileHashUpdater fileHashUpdater) {
		this._fileStatusUpdater = fileStatusUpdater;
		this._fileHashUpdater = fileHashUpdater;
		this.FileStatusUpdaterTargetCount = this._fileStatusUpdater.TargetCount.ThrottleLast(TimeSpan.FromMilliseconds(100)).ObserveOnCurrentSynchronizationContext().ToBindableReactiveProperty();
		this.FileStatusUpdaterCompletedCount = this._fileStatusUpdater.CompletedCount.ThrottleLast(TimeSpan.FromMilliseconds(100)).ObserveOnCurrentSynchronizationContext().ToBindableReactiveProperty();
		this.FileHashUpdaterTargetCount = this._fileHashUpdater.TargetCount.ThrottleLast(TimeSpan.FromMilliseconds(100)).ObserveOnCurrentSynchronizationContext().ToBindableReactiveProperty();
		this.FileHashUpdaterCompletedCount = this._fileHashUpdater.CompletedCount.ThrottleLast(TimeSpan.FromMilliseconds(100)).ObserveOnCurrentSynchronizationContext().ToBindableReactiveProperty();
		this.FullHashUpdaterTargetCount = this._fileHashUpdater.FullHashTargetCount.ThrottleLast(TimeSpan.FromMilliseconds(100)).ObserveOnCurrentSynchronizationContext().ToBindableReactiveProperty();
		this.FullHashUpdaterCompletedCount = this._fileHashUpdater.FullHashCompletedCount.ThrottleLast(TimeSpan.FromMilliseconds(100)).ObserveOnCurrentSynchronizationContext().ToBindableReactiveProperty();
		this.Actions.Synchronize().ObserveOnThreadPool().Subscribe(action => action());
	}

	private readonly FileStatusUpdater _fileStatusUpdater;
	private readonly FileHashUpdater _fileHashUpdater;

	public BindableReactiveProperty<long> FileStatusUpdaterTargetCount {
		get;
	}

	public BindableReactiveProperty<long> FileStatusUpdaterCompletedCount {
		get;
	}

	public BindableReactiveProperty<long> FileHashUpdaterTargetCount {
		get;
	}

	public BindableReactiveProperty<long> FileHashUpdaterCompletedCount {
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
