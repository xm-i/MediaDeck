using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.MediaItemTypes.Base.Models;

namespace MediaDeck.MediaItemTypes.Image.Models;

public class ImageMediaItemModel(long id, string filePath, ImageMediaItemOperator fileOperator, IMediaItemTypeProvider mediaItemTypeProvider, IServiceProvider scopedServiceProvider) : BaseMediaItemModel(id, filePath, fileOperator, MediaType.Image, mediaItemTypeProvider, scopedServiceProvider);