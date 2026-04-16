using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.FileTypes.Base.Models;

namespace MediaDeck.FileTypes.Image.Models;

internal class ImageFileModel(long id, string filePath, ImageFileOperator fileOperator, ConfigModel config) : BaseFileModel(id, filePath, fileOperator, MediaType.Image, config);