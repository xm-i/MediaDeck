using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.MediaItemTypes.Base.ViewModels;

namespace MediaDeck.MediaItemTypes.FolderGroup.ViewModels;

/// <summary>
/// フォルダグループのメディアアイテムViewModel
/// </summary>
[Inject(InjectServiceLifetime.Transient)]
public class FolderGroupMediaItemViewModel(IMediaItemFactoryOf<FolderGroupMediaItemViewModel> mediaItemFactory) : BaseMediaItemViewModel(mediaItemFactory, MediaType.FolderGroup);