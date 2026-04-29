using MediaDeck.Composition.Enum;
using MediaDeck.MediaItemTypes.Base.Models;

namespace MediaDeck.MediaItemTypes.FolderGroup.Models;

/// <summary>
/// フォルダグループのメディアアイテムモデル
/// </summary>
[Inject(InjectServiceLifetime.Transient)]
public class FolderGroupMediaItemModel(FolderGroupMediaItemOperator fileOperator, FolderGroupMediaItemTypeProvider mediaItemTypeProvider, IServiceProvider scopedServiceProvider) : BaseMediaItemModel(fileOperator, MediaType.FolderGroup, mediaItemTypeProvider, scopedServiceProvider);