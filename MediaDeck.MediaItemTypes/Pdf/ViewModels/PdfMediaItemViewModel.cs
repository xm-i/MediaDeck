using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.MediaItemTypes.Base.ViewModels;

namespace MediaDeck.MediaItemTypes.Pdf.ViewModels;

[Inject(InjectServiceLifetime.Transient)]
public class PdfMediaItemViewModel(IMediaItemFactoryOf<PdfMediaItemViewModel> mediaItemFactory) : BaseMediaItemViewModel(mediaItemFactory, MediaType.Pdf);