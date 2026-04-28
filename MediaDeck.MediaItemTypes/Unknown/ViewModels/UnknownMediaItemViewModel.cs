using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;
using MediaDeck.MediaItemTypes.Base.ViewModels;

namespace MediaDeck.MediaItemTypes.Unknown.ViewModels;

public class UnknownMediaItemViewModel(IMediaItemModel fileModel, IMediaItemFactory mediaItemFactory) : BaseMediaItemViewModel(fileModel, mediaItemFactory, MediaType.Unknown) {
}