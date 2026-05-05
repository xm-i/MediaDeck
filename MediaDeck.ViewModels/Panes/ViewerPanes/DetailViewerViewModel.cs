using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Core.Models.Files;

namespace MediaDeck.ViewModels.Panes.ViewerPanes;

[Inject(InjectServiceLifetime.Transient)]
public class DetailViewerViewModel : ViewerPaneViewModelBase, IDetailViewerViewModel {
	public DetailViewerViewModel(MediaContentLibraryViewModel mediaContentLibraryViewModel, FilesManager filesManager) : base(ViewerType.Detail, "Detail", "\uE954", filesManager) {
		this.MediaContentLibraryViewModel = mediaContentLibraryViewModel;
		mediaContentLibraryViewModel.SelectedFile.Subscribe(x => {
			this.SelectedFile.Value = x;
			this.SelectedFilePath.Value = x?.FilePath;
			this.SelectedFileThumbnailFilePath.Value = x?.ThumbnailFilePath.Value;
		}).AddTo(this.CompositeDisposable);
	}


	public MediaContentLibraryViewModel MediaContentLibraryViewModel {
		get;
	}


	/// <inheritdoc/>
	public BindableReactiveProperty<IMediaItemViewModel?> SelectedFile {
		get;
	} = new(null);

	/// <inheritdoc/>
	public BindableReactiveProperty<string?> SelectedFilePath {
		get;
	} = new(null);

	/// <inheritdoc/>
	public BindableReactiveProperty<string?> SelectedFileThumbnailFilePath {
		get;
	} = new(null);
}