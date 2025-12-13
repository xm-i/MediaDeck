using MediaDeck.Composition.Bases;
using MediaDeck.Models.Repositories;

namespace MediaDeck.ViewModels.Panes.RepositoryPanes;

[Inject(InjectServiceLifetime.Transient)]
public class RepositorySelectorViewModel: ViewModelBase {

	public RepositorySelectorViewModel(
		RepositorySelector repositorySelector) {
		this.RepositoryPaneViewModels = repositorySelector.Repositories.Select(x => x switch {
			FolderRepository folderRepository => new FolderRepositoryViewModel(folderRepository),
			_ => throw new NotImplementedException()
		}).ToArray();
		this.FolderRepositoryViewModel = (this.RepositoryPaneViewModels.First(vm => vm is FolderRepositoryViewModel) as FolderRepositoryViewModel)!;
		this.SelectedRepositoryPane = repositorySelector.SelectedRepository.Select(x => this.RepositoryPaneViewModels.First(vm => vm.Model == x)).ToBindableReactiveProperty(null!);
		this.LoadCommand.Subscribe(async _ => {
			foreach(var repository in repositorySelector.Repositories) {
				await repository.Load();
			}
		});
	}

	public ReactiveCommand LoadCommand {
		get;
	} = new();

	public BindableReactiveProperty<RepositoryViewModelBase> SelectedRepositoryPane {
		get;
	} = new();

	public RepositoryViewModelBase[] RepositoryPaneViewModels {
		get;
	}

	public FolderRepositoryViewModel FolderRepositoryViewModel {
		get;
	}
}
