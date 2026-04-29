using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.MediaItemTypes.Base.ViewModels;

namespace MediaDeck.MediaItemTypes.Archive.ViewModels;

[Inject(InjectServiceLifetime.Transient)]
public class ArchiveMediaItemViewModel(IMediaItemFactoryOf<ArchiveMediaItemViewModel> mediaItemFactory) : BaseMediaItemViewModel(mediaItemFactory, MediaType.Archive);