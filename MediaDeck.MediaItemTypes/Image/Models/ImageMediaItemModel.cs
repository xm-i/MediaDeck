using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.MediaItemTypes.Base.Models;

namespace MediaDeck.MediaItemTypes.Image.Models;

internal class ImageMediaItemModel(long id, string filePath, ImageMediaItemOperator fileOperator, IMediaItemType mediaItemType, IServiceProvider scopedServiceProvider) : BaseMediaItemModel(id, filePath, fileOperator, MediaType.Image, mediaItemType, scopedServiceProvider);