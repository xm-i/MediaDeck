namespace MediaDeck.Models.Repositories;
[Inject(InjectServiceLifetime.Singleton)]
public class RepositorySelector {

	public RepositorySelector(FolderRepository folderRepository) {
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
