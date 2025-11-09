using MediaDeck.Models.Preferences;

namespace MediaDeck.Models.Repositories;
[AddSingleton]
public class RepositorySelector {

	public RepositorySelector(FolderRepository folderRepository,States states) {
		this.Repositories = [
			folderRepository
		];
		this.SelectedRepository.Value = folderRepository;
	}
	public RepositoryBase[] Repositories {
		get;
	}

	public ReactiveProperty<RepositoryBase> SelectedRepository {
		get;
	} = new();
}
