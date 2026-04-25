using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.MediaItemTypes.Base.Models;

namespace MediaDeck.MediaItemTypes.Video.Models;

internal class VideoMediaItemModel(long id, string filePath, VideoMediaItemOperator fileOperator, ConfigModel config) : BaseMediaItemModel(id, filePath, fileOperator, MediaType.Video, config);