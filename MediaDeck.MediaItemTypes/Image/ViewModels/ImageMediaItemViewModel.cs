using MediaDeck.Composition.Enum;
using MediaDeck.MediaItemTypes.Base.ViewModels;

namespace MediaDeck.MediaItemTypes.Image.ViewModels;

[Inject(InjectServiceLifetime.Transient)]
public class ImageMediaItemViewModel() : BaseMediaItemViewModel(MediaType.Image);