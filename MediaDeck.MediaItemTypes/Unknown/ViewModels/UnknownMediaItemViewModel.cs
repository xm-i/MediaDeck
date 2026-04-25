using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;
using MediaDeck.MediaItemTypes.Base.ViewModels;

namespace MediaDeck.MediaItemTypes.Unknown.ViewModels;

internal class UnknownMediaItemViewModel(IMediaItemModel fileModel) : BaseMediaItemViewModel(fileModel, MediaType.Unknown);