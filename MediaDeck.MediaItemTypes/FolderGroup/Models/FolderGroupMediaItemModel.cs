using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.MediaItemTypes.Base.Models;

namespace MediaDeck.MediaItemTypes.FolderGroup.Models;

/// <summary>
/// フォルダグループのメディアアイテムモデル
/// </summary>
public class FolderGroupMediaItemModel(long id, string filePath, FolderGroupMediaItemOperator fileOperator, IMediaItemTypeProvider mediaItemTypeProvider, IServiceProvider scopedServiceProvider) : BaseMediaItemModel(id, filePath, fileOperator, MediaType.FolderGroup, mediaItemTypeProvider, scopedServiceProvider);