using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;
using MediaDeck.MediaItemTypes.Base.ViewModels;

namespace MediaDeck.MediaItemTypes.Image.ViewModels;

public class ImageMediaItemViewModel(IMediaItemModel fileModel, IMediaItemFactoryCore mediaItemFactory) : BaseMediaItemViewModel(fileModel, mediaItemFactory, MediaType.Image) {
}