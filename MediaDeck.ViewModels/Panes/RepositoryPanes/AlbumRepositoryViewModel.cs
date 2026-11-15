using MediaDeck.Common.Extensions;
using MediaDeck.Core.Models.Repositories;
using MediaDeck.Core.Models.Repositories.Objects;

namespace MediaDeck.ViewModels.Panes.RepositoryPanes;

/// <summary>
/// アルバムリポジトリペインの ViewModel。
/// </summary>
[Inject(InjectServiceLifetime.Scoped)]
public class AlbumRepositoryViewModel : RepositoryViewModelBase {
	/// <summary>
	/// <see cref="AlbumRepositoryViewModel"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="albumRepository">アルバムリポジトリ。</param>
	public AlbumRepositoryViewModel(AlbumRepository albumRepository) : base("Album", albumRepository) {
		this.AlbumRepository = albumRepository;
		this.RootAlbum = albumRepository.RootAlbum.ObserveOnCurrentSynchronizationContext().ToBindableReactiveProperty(null!).AddTo(this.CompositeDisposable);
		this.IsEmpty = albumRepository.RootAlbum
			.Select(root => root is { ChildAlbums.Length: 0 })
			.ObserveOnCurrentSynchronizationContext()
			.ToBindableReactiveProperty(albumRepository.RootAlbum.Value is { ChildAlbums.Length: 0 })
			.AddTo(this.CompositeDisposable);
		this.SetRepositoryConditionCommand.Merge(this.IncludeSubAlbums.ToUnit())
			.Subscribe(_ => {
				if (this.SelectedAlbum.Value is not { } album) {
					return;
				}
				albumRepository.SetRepositoryCandidate(album, this.IncludeSubAlbums.Value);
			}).AddTo(this.CompositeDisposable);
	}

	/// <summary>
	/// アルバムリポジトリモデルを取得します。
	/// </summary>
	public AlbumRepository AlbumRepository {
		get;
	}

	/// <summary>
	/// ルートアルバムオブジェクトを取得します。
	/// </summary>
	public BindableReactiveProperty<AlbumObject> RootAlbum {
		get;
	}

	/// <summary>
	/// アルバムが1件も存在しないかどうかを取得します。
	/// </summary>
	public BindableReactiveProperty<bool> IsEmpty {
		get;
	}

	/// <summary>
	/// 選択中のアルバムを取得または設定します。
	/// </summary>
	public BindableReactiveProperty<AlbumObject?> SelectedAlbum {
		get;
	} = new();

	/// <summary>
	/// 選択中のアルバムを検索条件に設定するコマンドを取得します。
	/// </summary>
	public ReactiveCommand SetRepositoryConditionCommand {
		get;
	} = new();

	/// <summary>
	/// サブアルバムを検索条件に含めるかどうかを取得または設定します。
	/// </summary>
	public BindableReactiveProperty<bool> IncludeSubAlbums {
		get;
	} = new(true);

	/// <summary>
	/// 指定されたパスのアルバムを作成します。
	/// </summary>
	/// <param name="path">作成するアルバムのパス。</param>
	public Task CreateAlbumAsync(string path) {
		return this.AlbumRepository.GetOrCreateAsync(path);
	}

	/// <summary>
	/// 指定された単アルバムを削除します。子アルバムを持つ場合は削除しません。
	/// </summary>
	/// <param name="album">削除するアルバムオブジェクト。</param>
	public async Task DeleteAlbumAsync(AlbumObject album) {
		if (!album.CanDelete) {
			return;
		}
		var deleted = await this.AlbumRepository.DeleteAlbumAsync(album.AlbumPath);
		if (deleted && ReferenceEquals(this.SelectedAlbum.Value, album)) {
			this.SelectedAlbum.Value = null;
		}
	}
}