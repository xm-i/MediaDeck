using MediaDeck.Composition.Bases;
using MediaDeck.Models.FolderManager;

namespace MediaDeck.ViewModels.FolderManager;

[Inject(InjectServiceLifetime.Transient)]
public class FolderManagerViewModel: ViewModelBase {
	private readonly FolderManagerModel _folderManager;

	public INotifyCollectionChangedSynchronizedViewList<FolderViewModel> Folders {
		get;
	}

	public BindableReactiveProperty<FolderViewModel?> SelectedFolder {
		get;
	} = new();

	public ReactiveCommand<string> AddFolderCommand {
		get;
	} = new();
	public ReactiveCommand<FolderViewModel> RemoveFolderCommand {
		get;
	} = new();

	public ReactiveCommand ScanCommand {
		get;
	} = new();

	public ReactiveCommand<FolderViewModel> ScanSelectedFolderCommand {
		get;
	} = new();

	public FolderManagerViewModel(FolderManagerModel folderManager) {
		this._folderManager = folderManager;
		this.Folders = this._folderManager.Folders.CreateView(x => new FolderViewModel(x)).ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);
		this.AddFolderCommand.Subscribe(x => this._folderManager.AddFolder(x)).AddTo(this.CompositeDisposable);
		this.RemoveFolderCommand.Subscribe(x => this._folderManager.RemoveFolder(x.GetModel())).AddTo(this.CompositeDisposable);
		this.ScanCommand.Subscribe(async x => await this._folderManager.Scan()).AddTo(this.CompositeDisposable);
		this.ScanSelectedFolderCommand.Subscribe(async x => {
			if (x is not null) {
				await this._folderManager.ScanFolder(x.GetModel());
			}
		}).AddTo(this.CompositeDisposable);
	}
}
