using MediaDeck.Composition.Enum;
using MediaDeck.FileTypes.Base.Models;

namespace MediaDeck.FileTypes.Video.Models;

internal class VideoFileModel(long id, string filePath, VideoFileOperator fileOperator) : BaseFileModel(id, filePath, fileOperator, MediaType.Video);