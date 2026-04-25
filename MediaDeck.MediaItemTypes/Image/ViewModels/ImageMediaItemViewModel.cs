using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;
using MediaDeck.MediaItemTypes.Base.ViewModels;

namespace MediaDeck.MediaItemTypes.Image.ViewModels;

internal class ImageMediaItemViewModel(IMediaItemModel fileModel) : BaseMediaItemViewModel(fileModel, MediaType.Image) {
}