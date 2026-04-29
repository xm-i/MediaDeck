using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Views;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.MediaItemTypes.Archive;
using MediaDeck.MediaItemTypes.Archive.Models;
using MediaDeck.MediaItemTypes.Archive.ViewModels;
using MediaDeck.MediaItemTypes.Base.Models;
using MediaDeck.MediaItemTypes.Base.ViewModels;
using MediaDeck.MediaItemTypes.UI.Archive.Views;

namespace MediaDeck.MediaItemTypes.UI.Archive;

[Inject(InjectServiceLifetime.Scoped, typeof(IMediaItemFactory))]
[Inject(InjectServiceLifetime.Scoped, typeof(IMediaItemFactoryOf<ArchiveMediaItemViewModel>))]
public class ArchiveMediaItemFactory :
	ArchiveMediaItemFactoryCore,
	IMediaItemFactory<ArchiveMediaItemOperator, ArchiveMediaItemModel, DefaultExecutionProgramObjectModel, ArchiveMediaItemViewModel, DefaultExecutionProgramConfigViewModel, ArchiveThumbnailPickerViewModel>,
	IMediaItemFactoryOf<ArchiveMediaItemViewModel> {
	public ArchiveMediaItemFactory(
		ArchiveMediaItemOperator ArchiveMediaItemOperator,
		ConfigModel config,
		ITagsManager tagsManager,
		IServiceProvider serviceProvider) : base(ArchiveMediaItemOperator, config, tagsManager, serviceProvider) {
	}

	public IThumbnailControlView CreateThumbnailControlView(ArchiveMediaItemViewModel fileViewModel) {
		return new ArchiveThumbnailControlView { DataContext = fileViewModel };
	}

	public IThumbnailControlView CreateThumbnailControlView(IMediaItemViewModel fileViewModel) {
		return this.CreateThumbnailControlView((ArchiveMediaItemViewModel)fileViewModel);
	}
}
