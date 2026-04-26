using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.MediaItemTypes.Base.Models;

namespace MediaDeck.MediaItemTypes.FolderGroup.Models;

/// <summary>
/// フォルダグループのメディアアイテムモデル
/// </summary>
internal class FolderGroupMediaItemModel(long id, string filePath, FolderGroupMediaItemOperator fileOperator, IMediaItemType mediaItemType, IServiceProvider scopedServiceProvider) : BaseMediaItemModel(id, filePath, fileOperator, MediaType.FolderGroup, mediaItemType, scopedServiceProvider);