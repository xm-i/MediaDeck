using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.MediaItemTypes.Base.Models;

namespace MediaDeck.MediaItemTypes.Video.Models;

public class VideoMediaItemModel(long id, string filePath, VideoMediaItemOperator fileOperator, IMediaItemFactory mediaItemFactory, IServiceProvider scopedServiceProvider) : BaseMediaItemModel(id, filePath, fileOperator, MediaType.Video, mediaItemFactory, scopedServiceProvider);