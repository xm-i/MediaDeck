using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.MediaItemTypes.Base.Models;

namespace MediaDeck.MediaItemTypes.Archive.Models;

internal class ArchiveMediaItemModel(long id, string filePath, ArchiveMediaItemOperator fileOperator, ConfigModel config) : BaseMediaItemModel(id, filePath, fileOperator, MediaType.Archive, config);