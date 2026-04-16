using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.FileTypes.Base.Models;

namespace MediaDeck.FileTypes.Unknown.Models;

internal class UnknownFileModel(long id, string filePath, UnknownFileOperator fileOperator, ConfigModel config) : BaseFileModel(id, filePath, fileOperator, MediaType.Unknown, config);