using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.MediaItemTypes.Base.Models;

namespace MediaDeck.MediaItemTypes.FolderGroup.Models;

/// <summary>
/// フォルダグループのメディアアイテムモデル
/// </summary>
[Inject(InjectServiceLifetime.Transient)]
public class FolderGroupMediaItemModel(FolderGroupMediaItemOperator fileOperator, IMediaItemTypeProvider mediaItemTypeProvider, IServiceProvider scopedServiceProvider) : BaseMediaItemModel(fileOperator, MediaType.FolderGroup, mediaItemTypeProvider, scopedServiceProvider);