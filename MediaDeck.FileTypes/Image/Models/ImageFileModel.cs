using MediaDeck.Composition.Enum;
using MediaDeck.FileTypes.Base.Models;

namespace MediaDeck.FileTypes.Image.Models;

internal class ImageFileModel(long id, string filePath, ImageFileOperator fileOperator) : BaseFileModel(id, filePath, fileOperator, MediaType.Image);