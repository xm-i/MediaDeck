using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.MediaItemTypes.Base.Models;

namespace MediaDeck.MediaItemTypes.Video.Models;

internal class VideoMediaItemModel(long id, string filePath, VideoMediaItemOperator fileOperator, IMediaItemType mediaItemType, IServiceProvider scopedServiceProvider) : BaseMediaItemModel(id, filePath, fileOperator, MediaType.Video, mediaItemType, scopedServiceProvider);