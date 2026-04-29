using MediaDeck.Composition.Enum;
using MediaDeck.MediaItemTypes.Base.ViewModels;

namespace MediaDeck.MediaItemTypes.Archive.ViewModels;

[Inject(InjectServiceLifetime.Transient)]
public class ArchiveMediaItemViewModel() : BaseMediaItemViewModel(MediaType.Archive);