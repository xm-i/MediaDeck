using MediaDeck.Common.Extensions;
using MediaDeck.Core.Models.Repositories;
using MediaDeck.Core.Models.Repositories.Objects;

namespace MediaDeck.ViewModels.Panes.RepositoryPanes;

[Inject(InjectServiceLifetime.Scoped)]
public class FolderRepositoryViewModel : RepositoryViewModelBase {
	public FolderRepositoryViewModel(FolderRepository folderRepository) : base("Folder", folderRepository) {
		this.RootFolder = folderRepository.RootFolder.ObserveOnCurrentSynchronizationContext().ToBindableReactiveProperty(null!);
		this.SetRepositoryConditionCommand.Merge(this.IncludeSubDirectories.ToUnit())
			.Subscribe(_ => {
				if (this.SelectedFolder.Value is not { } folder) {
					return;
				}
				folderRepository.SetRepositoryCandidate(folder, this.IncludeSubDirectories.Value);
			}).AddTo(this.CompositeDisposable);
	}

	public BindableReactiveProperty<FolderObject> RootFolder {
		get;
	}

	public BindableReactiveProperty<FolderObject?> SelectedFolder {
		get;
	} = new();

	public ReactiveCommand SetRepositoryConditionCommand {
		get;
	} = new();

	public BindableReactiveProperty<bool> IncludeSubDirectories {
		get;
	} = new(true);
}