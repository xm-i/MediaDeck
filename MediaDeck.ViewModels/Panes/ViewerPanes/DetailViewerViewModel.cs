using MediaDeck.Composition.Interfaces.FileTypes;
using MediaDeck.Composition.Interfaces.FileTypes.ViewModels;
using MediaDeck.Composition.Interfaces.FileTypes.Views;
using MediaDeck.Core.Models.Files;

namespace MediaDeck.ViewModels.Panes.ViewerPanes;

[Inject(InjectServiceLifetime.Transient)]
public class DetailViewerViewModel : ViewerPaneViewModelBase, IDetailViewerViewModel {
	public DetailViewerViewModel(MediaContentLibraryViewModel mediaContentLibraryViewModel, FilesManager filesManager, IFileTypeService fileTypeService) : base("Detail", filesManager) {
		this.MediaContentLibraryViewModel = mediaContentLibraryViewModel;
		mediaContentLibraryViewModel.SelectedFile.Subscribe(x => {
			if (x is not { } vm) {
				this.DetailViewerPreviewControlView.Value = null;
				return;
			}
			this.DetailViewerPreviewControlView.Value = fileTypeService.CreateDetailViewerPreviewControlView(vm);
			this.DetailViewerPreviewControlView.Value.DataContext = this;
			this.SelectedFile.Value = vm;
			this.SelectedFilePath.Value = vm.FilePath;
			this.SelectedFileThumbnailFilePath.Value = vm.ThumbnailFilePath.Value;
		}).AddTo(this.CompositeDisposable);
	}


	public MediaContentLibraryViewModel MediaContentLibraryViewModel {
		get;
	}

	/// <summary>
	/// 各ファイルタイプのDetailViewerView。
	/// </summary>
	public BindableReactiveProperty<IDetailViewerPreviewControlView?> DetailViewerPreviewControlView {
		get;
	} = new();

	/// <inheritdoc/>
	public BindableReactiveProperty<IFileViewModel?> SelectedFile {
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