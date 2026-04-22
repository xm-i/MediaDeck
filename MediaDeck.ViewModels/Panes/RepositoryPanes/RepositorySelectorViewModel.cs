using MediaDeck.Common.Base;
using MediaDeck.Core.Models.Repositories;

namespace MediaDeck.ViewModels.Panes.RepositoryPanes;

[Inject(InjectServiceLifetime.Scoped)]
public class RepositorySelectorViewModel : ViewModelBase {
	public RepositorySelectorViewModel(
		RepositorySelector repositorySelector, FolderRepositoryViewModel folderRepositoryViewModel) {
		this.RepositoryPaneViewModels = [folderRepositoryViewModel];
		this.FolderRepositoryViewModel = (this.RepositoryPaneViewModels.First(vm => vm is FolderRepositoryViewModel) as FolderRepositoryViewModel)!;
		this.SelectedRepositoryPane = repositorySelector.SelectedRepository.Select(x => this.RepositoryPaneViewModels.First(vm => vm.Model == x)).ToBindableReactiveProperty(null!);
		this.LoadCommand.Subscribe(async _ => {
			foreach (var repository in repositorySelector.Repositories) {
				await repository.Load();
			}
		}).AddTo(this.CompositeDisposable);
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