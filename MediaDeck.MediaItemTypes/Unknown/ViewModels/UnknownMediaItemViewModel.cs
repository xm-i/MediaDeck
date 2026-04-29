using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.MediaItemTypes.Base.ViewModels;

namespace MediaDeck.MediaItemTypes.Unknown.ViewModels;

[Inject(InjectServiceLifetime.Transient)]
public class UnknownMediaItemViewModel(IMediaItemFactoryOf<UnknownMediaItemViewModel> mediaItemFactory) : BaseMediaItemViewModel(mediaItemFactory, MediaType.Unknown);