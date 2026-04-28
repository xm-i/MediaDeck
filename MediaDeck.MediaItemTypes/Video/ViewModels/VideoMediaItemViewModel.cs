using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;
using MediaDeck.MediaItemTypes.Base.ViewModels;

namespace MediaDeck.MediaItemTypes.Video.ViewModels;

public class VideoMediaItemViewModel(IMediaItemModel fileModel, IMediaItemFactory mediaItemFactory) : BaseMediaItemViewModel(fileModel, mediaItemFactory, MediaType.Video);