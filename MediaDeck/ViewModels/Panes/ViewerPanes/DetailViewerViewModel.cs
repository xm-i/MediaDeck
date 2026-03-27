using MediaDeck.Composition.Interfaces.FileTypes.ViewModels;
using MediaDeck.Composition.Interfaces.FileTypes.Views;
using MediaDeck.Models.FileDetailManagers;

namespace MediaDeck.ViewModels.Panes.ViewerPanes;

[Inject(InjectServiceLifetime.Transient)]
public class DetailViewerViewModel : ViewerPaneViewModelBase, IDetailViewerViewModel {
	public DetailViewerViewModel(MediaContentLibraryViewModel mediaContentLibraryViewModel, FilesManager filesManager) : base ("Detail", filesManager){
		this.MediaContentLibraryViewModel = mediaContentLibraryViewModel;
		mediaContentLibraryViewModel.SelectedFile.Subscribe(x => {
			if (x is not {} vm) {
				this.DetailViewerPreviewControlView.Value = null;
				return;
			}
			this.DetailViewerPreviewControlView.Value = FileTypeUtility.CreateDetailViewerPreviewControlView(vm);
			this.DetailViewerPreviewControlView.Value.DataContext = this;
			this.SelectedFilePath.Value = vm.FilePath;
			this.SelectedFileThumbnailFilePath.Value = vm.ThumbnailFilePath.Value;
		});
	}

	public MediaContentLibraryViewModel MediaContentLibraryViewModel {
		get;
	}

	public BindableReactiveProperty<IDetailViewerPreviewControlView?> DetailViewerPreviewControlView {
		get;
	} = new();

	public BindableReactiveProperty<string?> SelectedFilePath {
		get;
	} = new(null);

	public BindableReactiveProperty<string?> SelectedFileThumbnailFilePath {
		get;
	} = new(null);
}
