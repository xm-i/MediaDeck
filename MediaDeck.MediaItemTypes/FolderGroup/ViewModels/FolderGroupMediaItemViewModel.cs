using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;
using MediaDeck.MediaItemTypes.Base.ViewModels;

namespace MediaDeck.MediaItemTypes.FolderGroup.ViewModels;

/// <summary>
/// フォルダグループのメディアアイテムViewModel
/// </summary>
internal class FolderGroupMediaItemViewModel(IMediaItemModel fileModel) : BaseMediaItemViewModel(fileModel, MediaType.FolderGroup);