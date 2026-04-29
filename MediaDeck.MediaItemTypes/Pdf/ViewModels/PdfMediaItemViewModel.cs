using MediaDeck.Composition.Enum;
using MediaDeck.MediaItemTypes.Base.ViewModels;

namespace MediaDeck.MediaItemTypes.Pdf.ViewModels;

[Inject(InjectServiceLifetime.Transient)]
public class PdfMediaItemViewModel() : BaseMediaItemViewModel(MediaType.Pdf);