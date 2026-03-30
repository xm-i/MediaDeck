using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.FileTypes.Models;
using MediaDeck.FileTypes.Base.ViewModels;

namespace MediaDeck.FileTypes.Pdf.ViewModels;

internal class PdfFileViewModel(IFileModel fileModel) : BaseFileViewModel(fileModel, MediaType.Pdf);