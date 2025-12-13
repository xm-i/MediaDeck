using MediaDeck.Composition.Bases;
using MediaDeck.Models.Files;
using MediaDeck.Models.FolderManager;

namespace MediaDeck.ViewModels.FolderManager;

[Inject(InjectServiceLifetime.Transient)]
public class FolderManagerViewModel: ViewModelBase {
	private readonly FolderManagerModel _folderManager;

	public INotifyCollectionChangedSynchronizedViewList<FolderViewModel> Folders {
		get;
	}

	public ReactiveCommand<string> AddFolderCommand {
		get;
	} = new();
	public ReactiveCommand<FolderViewModel> RemoveFolderCommand {
		get;
	} = new();

	public ReactiveCommand ScanCommand {
		get;
	} = new();

	public BindableReactiveProperty<long> QueueCount {
		get;
	}

	public BindableReactiveProperty<bool> IsScanning {
		get;
	}

	public FolderManagerViewModel(FolderManagerModel folderManager, FileRegistrar fileRegistrar) {
		this._folderManager = folderManager;
		this.Folders = this._folderManager.Folders.CreateView(x => new FolderViewModel(x)).ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);
		this.AddFolderCommand.Subscribe(x => this._folderManager.AddFolder(x)).AddTo(this.CompositeDisposable);
		this.RemoveFolderCommand.Subscribe(x => this._folderManager.RemoveFolder(x.GetModel())).AddTo(this.CompositeDisposable);
		this.ScanCommand.Subscribe(async x => await this._folderManager.Scan()).AddTo(this.CompositeDisposable);
		this.QueueCount = fileRegistrar.QueueCount.ObserveOnCurrentSynchronizationContext().ToBindableReactiveProperty();
		this.IsScanning = fileRegistrar.QueueCount.ObserveOnCurrentSynchronizationContext().Select(x => x > 0).ToBindableReactiveProperty();
	}
}
