using MediaDeck.Composition.Enum;
using MediaDeck.MediaItemTypes.Base.Models;

namespace MediaDeck.MediaItemTypes.Video.Models;

[Inject(InjectServiceLifetime.Transient)]
public class VideoMediaItemModel(VideoMediaItemOperator fileOperator, VideoMediaItemTypeProvider mediaItemTypeProvider, IServiceProvider scopedServiceProvider) : BaseMediaItemModel(fileOperator, MediaType.Video, mediaItemTypeProvider, scopedServiceProvider);