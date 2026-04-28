using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.MediaItemTypes.Base.Models;

namespace MediaDeck.MediaItemTypes.Video.Models;

public class VideoMediaItemModel(long id, string filePath, VideoMediaItemOperator fileOperator, IMediaItemTypeProvider mediaItemTypeProvider, IServiceProvider scopedServiceProvider) : BaseMediaItemModel(id, filePath, fileOperator, MediaType.Video, mediaItemTypeProvider, scopedServiceProvider);