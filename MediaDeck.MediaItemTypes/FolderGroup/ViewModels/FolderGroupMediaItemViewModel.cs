using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;
using MediaDeck.MediaItemTypes.Base.ViewModels;

namespace MediaDeck.MediaItemTypes.FolderGroup.ViewModels;

/// <summary>
/// フォルダグループのメディアアイテムViewModel
/// </summary>
public class FolderGroupMediaItemViewModel(IMediaItemModel fileModel, IMediaItemType mediaItemType) : BaseMediaItemViewModel(fileModel, mediaItemType, MediaType.FolderGroup);