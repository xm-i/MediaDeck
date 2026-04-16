using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.FileTypes.Base.Models;

namespace MediaDeck.FileTypes.Video.Models;

internal class VideoFileModel(long id, string filePath, VideoFileOperator fileOperator, ConfigModel config) : BaseFileModel(id, filePath, fileOperator, MediaType.Video, config);