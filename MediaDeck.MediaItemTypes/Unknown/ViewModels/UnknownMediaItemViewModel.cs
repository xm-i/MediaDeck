using MediaDeck.Composition.Enum;
using MediaDeck.MediaItemTypes.Base.ViewModels;

namespace MediaDeck.MediaItemTypes.Unknown.ViewModels;

[Inject(InjectServiceLifetime.Transient)]
public class UnknownMediaItemViewModel() : BaseMediaItemViewModel(MediaType.Unknown);