using System.Threading.Tasks;

using MediaDeck.Composition.Bases;
using MediaDeck.Models.Tools;

namespace MediaDeck.ViewModels.Tools;
[AddSingleton]
public class BackgroundTasksViewModel: ViewModelBase {

	public BackgroundTasksViewModel(FileStatusUpdater fileStatusUpdater) {
		this._fileStatusUpdater = fileStatusUpdater;
		this.FileStatusUpdaterTargetCount = this._fileStatusUpdater.TargetCount.ThrottleLast(TimeSpan.FromMilliseconds(100)).ObserveOnCurrentSynchronizationContext().ToBindableReactiveProperty();
		this.FileStatusUpdaterCompletedCount = this._fileStatusUpdater.CompletedCount.ThrottleLast(TimeSpan.FromMilliseconds(100)).ObserveOnCurrentSynchronizationContext().ToBindableReactiveProperty();
		this.Actions.Synchronize().ObserveOnThreadPool().Subscribe(action => action());
	}

	private readonly FileStatusUpdater _fileStatusUpdater;

	public BindableReactiveProperty<long> FileStatusUpdaterTargetCount {
		get;
	}

	public BindableReactiveProperty<long> FileStatusUpdaterCompletedCount {
		get;
	}

	public Subject<Action> Actions {
		get;
	} = new();

	public void Start() {
		this.Actions.OnNext(async () => await this._fileStatusUpdater.UpdateFileInfo());
	}
}
