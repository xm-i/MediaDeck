using MediaDeck.Common.Extensions;
using MediaDeck.Core.Models.Repositories;
using MediaDeck.Core.Models.Repositories.Objects;

namespace MediaDeck.ViewModels.Panes.RepositoryPanes;

[Inject(InjectServiceLifetime.Scoped)]
public class AlbumRepositoryViewModel : RepositoryViewModelBase {
	public AlbumRepositoryViewModel(AlbumRepository albumRepository) : base("Album", albumRepository) {
		this.AlbumRepository = albumRepository;
		this.RootAlbum = albumRepository.RootAlbum.ObserveOnCurrentSynchronizationContext().ToBindableReactiveProperty(null!).AddTo(this.CompositeDisposable);
		this.SetRepositoryConditionCommand.Merge(this.IncludeSubAlbums.ToUnit())
			.Subscribe(_ => {
				if (this.SelectedAlbum.Value is not { } album) {
					return;
				}
				albumRepository.SetRepositoryCandidate(album, this.IncludeSubAlbums.Value);
			}).AddTo(this.CompositeDisposable);
	}

	public AlbumRepository AlbumRepository {
		get;
	}

	public BindableReactiveProperty<AlbumObject> RootAlbum {
		get;
	}

	public BindableReactiveProperty<AlbumObject?> SelectedAlbum {
		get;
	} = new();

	public ReactiveCommand SetRepositoryConditionCommand {
		get;
	} = new();

	public BindableReactiveProperty<bool> IncludeSubAlbums {
		get;
	} = new(true);
}