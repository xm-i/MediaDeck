using MediaDeck.Composition.Enum;
using MediaDeck.MediaItemTypes.Base.Models;

namespace MediaDeck.MediaItemTypes.Image.Models;

[Inject(InjectServiceLifetime.Transient)]
public class ImageMediaItemModel(ImageMediaItemOperator fileOperator, ImageMediaItemTypeProvider mediaItemTypeProvider, IServiceProvider scopedServiceProvider) : BaseMediaItemModel(fileOperator, MediaType.Image, mediaItemTypeProvider, scopedServiceProvider);