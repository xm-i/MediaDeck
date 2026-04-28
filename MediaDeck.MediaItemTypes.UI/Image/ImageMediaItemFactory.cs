using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Views;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.MediaItemTypes.Base.Models;
using MediaDeck.MediaItemTypes.Base.ViewModels;
using MediaDeck.MediaItemTypes.Image;
using MediaDeck.MediaItemTypes.Image.Models;
using MediaDeck.MediaItemTypes.Image.ViewModels;
using MediaDeck.MediaItemTypes.UI.Base.Views;
using MediaDeck.MediaItemTypes.UI.Image.Views;

namespace MediaDeck.MediaItemTypes.UI.Image;

[Inject(InjectServiceLifetime.Singleton, typeof(IMediaItemFactory))]
public class ImageMediaItemFactory : ImageMediaItemFactoryCore,
	IMediaItemFactory<ImageMediaItemOperator, ImageMediaItemModel, DefaultExecutionProgramObjectModel, ImageMediaItemViewModel, DefaultExecutionProgramConfigViewModel, ImageDetailViewerPreviewControlView, ImageThumbnailPickerViewModel, ImageThumbnailPickerView, DefaultExecutionConfigView> {
	private ImageDetailViewerPreviewControlView? _imageDetailViewerPreviewControlView;

	public ImageMediaItemFactory(
		ImageMediaItemOperator ImageMediaItemOperator,
		ConfigModel config,
		ITagsManager tagsManager,
		IMediaItemTypeProvider mediaItemTypeProvider,
		IServiceProvider serviceProvider)
		: base(ImageMediaItemOperator, config, tagsManager, mediaItemTypeProvider, serviceProvider) {
	}

	public ImageDetailViewerPreviewControlView CreateDetailViewerPreviewControlView(ImageMediaItemViewModel fileViewModel) {
		return this._imageDetailViewerPreviewControlView ??= new ImageDetailViewerPreviewControlView();
	}


	public IThumbnailControlView CreateThumbnailControlView(ImageMediaItemViewModel fileViewModel) {
		return new ImageThumbnailControlView { DataContext = fileViewModel };
	}

	public ImageThumbnailPickerView CreateThumbnailPickerView() {
		return new ImageThumbnailPickerView();
	}


	public DefaultExecutionConfigView CreateExecutionConfigView(DefaultExecutionProgramConfigViewModel viewModel) {
		return new DefaultExecutionConfigView() {
			ViewModel = viewModel
		};
	}

	public IDetailViewerPreviewControlView CreateDetailViewerPreviewControlView(IMediaItemViewModel fileViewModel) {
		return this.CreateDetailViewerPreviewControlView((ImageMediaItemViewModel)fileViewModel);
	}

	public IThumbnailControlView CreateThumbnailControlView(IMediaItemViewModel fileViewModel) {
		return this.CreateThumbnailControlView((ImageMediaItemViewModel)fileViewModel);
	}

	IThumbnailPickerView IMediaItemFactory.CreateThumbnailPickerView() {
		return this.CreateThumbnailPickerView();
	}

	public IExecutionConfigView CreateExecutionConfigView(IExecutionProgramConfigViewModel viewModel) {
		return this.CreateExecutionConfigView((DefaultExecutionProgramConfigViewModel)viewModel);
	}
}