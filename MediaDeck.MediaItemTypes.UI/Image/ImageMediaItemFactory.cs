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
using MediaDeck.MediaItemTypes.UI.Image.Views;

namespace MediaDeck.MediaItemTypes.UI.Image;

[Inject(InjectServiceLifetime.Scoped, typeof(IMediaItemFactory))]
[Inject(InjectServiceLifetime.Scoped, typeof(IMediaItemFactoryOf<ImageMediaItemViewModel>))]
public class ImageMediaItemFactory : ImageMediaItemFactoryCore,
	IMediaItemFactory<ImageMediaItemOperator, ImageMediaItemModel, DefaultExecutionProgramObjectModel, ImageMediaItemViewModel, DefaultExecutionProgramConfigViewModel, ImageThumbnailPickerViewModel>,
	IMediaItemFactoryOf<ImageMediaItemViewModel> {
	public ImageMediaItemFactory(
		ImageMediaItemOperator ImageMediaItemOperator,
		ConfigModel config,
		ITagsManager tagsManager,
		IServiceProvider serviceProvider)
		: base(ImageMediaItemOperator, config, tagsManager, serviceProvider) {
	}

	public IThumbnailControlView CreateThumbnailControlView(ImageMediaItemViewModel fileViewModel) {
		return new ImageThumbnailControlView { DataContext = fileViewModel };
	}

	public IThumbnailControlView CreateThumbnailControlView(IMediaItemViewModel fileViewModel) {
		return this.CreateThumbnailControlView((ImageMediaItemViewModel)fileViewModel);
	}
}
