using MediaDeck.Composition.Enum;
using MediaDeck.FileTypes.Base.Models;

namespace MediaDeck.FileTypes.Unknown.Models;

internal class UnknownFileModel(long id, string filePath, UnknownFileOperator fileOperator) : BaseFileModel(id, filePath, fileOperator, MediaType.Unknown);