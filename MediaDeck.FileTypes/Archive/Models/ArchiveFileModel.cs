using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.FileTypes.Base.Models;

namespace MediaDeck.FileTypes.Archive.Models;

internal class ArchiveFileModel(long id, string filePath, ArchiveFileOperator fileOperator, ConfigModel config) : BaseFileModel(id, filePath, fileOperator, MediaType.Archive, config);