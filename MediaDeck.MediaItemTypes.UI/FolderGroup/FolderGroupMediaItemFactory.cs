using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Views;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.MediaItemTypes.FolderGroup;
using MediaDeck.MediaItemTypes.FolderGroup.Models;
using MediaDeck.MediaItemTypes.FolderGroup.ViewModels;
using MediaDeck.MediaItemTypes.UI.FolderGroup.Views;

namespace MediaDeck.MediaItemTypes.UI.FolderGroup;

[Inject(InjectServiceLifetime.Scoped, typeof(IMediaItemFactory))]
[Inject(InjectServiceLifetime.Scoped, typeof(IMediaItemFactoryOf<FolderGroupMediaItemViewModel>))]
public class FolderGroupMediaItemFactory :
	FolderGroupMediaItemFactoryCore,
	IMediaItemFactory<FolderGroupMediaItemOperator, FolderGroupMediaItemModel, FolderGroupExecutionProgramObjectModel, FolderGroupMediaItemViewModel, FolderGroupExecutionProgramConfigViewModel, FolderGroupThumbnailPickerViewModel>,
	IMediaItemFactoryOf<FolderGroupMediaItemViewModel> {
	public FolderGroupMediaItemFactory(
		FolderGroupMediaItemOperator fileOperator,
		ConfigModel config,
		ITagsManager tagsManager,
		IServiceProvider serviceProvider)
		: base(fileOperator, config, tagsManager, serviceProvider) {
	}

	public IThumbnailControlView CreateThumbnailControlView(FolderGroupMediaItemViewModel fileViewModel) {
		return new FolderGroupThumbnailControlView { DataContext = fileViewModel };
	}

	public IThumbnailControlView CreateThumbnailControlView(IMediaItemViewModel fileViewModel) {
		return this.CreateThumbnailControlView((FolderGroupMediaItemViewModel)fileViewModel);
	}
}
