using MediaDeck.Composition.Enum;
using MediaDeck.MediaItemTypes.Base.ViewModels;

namespace MediaDeck.MediaItemTypes.Video.ViewModels;

[Inject(InjectServiceLifetime.Transient)]
public class VideoMediaItemViewModel() : BaseMediaItemViewModel(MediaType.Video);