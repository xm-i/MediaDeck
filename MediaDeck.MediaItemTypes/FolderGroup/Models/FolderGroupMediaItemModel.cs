using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.MediaItemTypes.Base.Models;

namespace MediaDeck.MediaItemTypes.FolderGroup.Models;

internal class FolderGroupMediaItemModel(long id, string filePath, FolderGroupMediaItemOperator fileOperator, ConfigModel config) : BaseMediaItemModel(id, filePath, fileOperator, MediaType.FolderGroup, config);