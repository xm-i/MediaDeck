using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Views;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.MediaItemTypes.Base.Models;
using MediaDeck.MediaItemTypes.Base.ViewModels;
using MediaDeck.MediaItemTypes.UI.Unknown.Views;
using MediaDeck.MediaItemTypes.Unknown;
using MediaDeck.MediaItemTypes.Unknown.Models;
using MediaDeck.MediaItemTypes.Unknown.ViewModels;

namespace MediaDeck.MediaItemTypes.UI.Unknown;

[Inject(InjectServiceLifetime.Scoped, typeof(IMediaItemFactory))]
[Inject(InjectServiceLifetime.Scoped, typeof(IMediaItemFactoryOf<UnknownMediaItemViewModel>))]
public class UnknownMediaItemFactory : UnknownMediaItemFactoryCore,
	IMediaItemFactory<UnknownMediaItemOperator, UnknownMediaItemModel, DefaultExecutionProgramObjectModel, UnknownMediaItemViewModel, DefaultExecutionProgramConfigViewModel, UnknownThumbnailPickerViewModel>,
	IMediaItemFactoryOf<UnknownMediaItemViewModel> {
	public UnknownMediaItemFactory(
		UnknownMediaItemOperator UnknownMediaItemOperator,
		ConfigModel config,
		ITagsManager tagsManager,
		IServiceProvider serviceProvider)
		: base(UnknownMediaItemOperator, config, tagsManager, serviceProvider) {
	}

	public IThumbnailControlView CreateThumbnailControlView(UnknownMediaItemViewModel fileViewModel) {
		return new UnknownThumbnailControlView { DataContext = fileViewModel };
	}

	public IThumbnailControlView CreateThumbnailControlView(IMediaItemViewModel fileViewModel) {
		return this.CreateThumbnailControlView((UnknownMediaItemViewModel)fileViewModel);
	}
}
