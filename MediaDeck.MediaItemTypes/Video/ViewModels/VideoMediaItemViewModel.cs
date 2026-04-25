using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;
using MediaDeck.MediaItemTypes.Base.ViewModels;

namespace MediaDeck.MediaItemTypes.Video.ViewModels;

internal class VideoMediaItemViewModel(IMediaItemModel fileModel) : BaseMediaItemViewModel(fileModel, MediaType.Video);