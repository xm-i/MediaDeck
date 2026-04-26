using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;
using MediaDeck.MediaItemTypes.Base.ViewModels;

namespace MediaDeck.MediaItemTypes.Archive.ViewModels;

public class ArchiveMediaItemViewModel(IMediaItemModel fileModel, IMediaItemType mediaItemType) : BaseMediaItemViewModel(fileModel, mediaItemType, MediaType.Archive);