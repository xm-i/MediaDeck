using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;
using MediaDeck.MediaItemTypes.Base.ViewModels;

namespace MediaDeck.MediaItemTypes.Image.ViewModels;

public class ImageMediaItemViewModel(IMediaItemModel fileModel, IMediaItemType mediaItemType) : BaseMediaItemViewModel(fileModel, mediaItemType, MediaType.Image) {
}