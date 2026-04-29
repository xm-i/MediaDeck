using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.MediaItemTypes.Base.ViewModels;

namespace MediaDeck.MediaItemTypes.Image.ViewModels;

[Inject(InjectServiceLifetime.Transient)]
public class ImageMediaItemViewModel(IMediaItemFactoryOf<ImageMediaItemViewModel> mediaItemFactory) : BaseMediaItemViewModel(mediaItemFactory, MediaType.Image);