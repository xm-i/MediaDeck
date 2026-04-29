using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.MediaItemTypes.Base.ViewModels;

namespace MediaDeck.MediaItemTypes.Video.ViewModels;

[Inject(InjectServiceLifetime.Transient)]
public class VideoMediaItemViewModel(IMediaItemFactoryOf<VideoMediaItemViewModel> mediaItemFactory) : BaseMediaItemViewModel(mediaItemFactory, MediaType.Video);