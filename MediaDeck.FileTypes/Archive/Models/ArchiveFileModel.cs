using MediaDeck.Composition.Enum;
using MediaDeck.FileTypes.Base.Models;

namespace MediaDeck.FileTypes.Archive.Models;

internal class ArchiveFileModel(long id, string filePath, ArchiveFileOperator fileOperator) : BaseFileModel(id, filePath, fileOperator, MediaType.Archive);