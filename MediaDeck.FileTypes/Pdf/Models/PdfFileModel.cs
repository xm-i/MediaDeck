using MediaDeck.Composition.Enum;
using MediaDeck.FileTypes.Base.Models;

namespace MediaDeck.FileTypes.Pdf.Models;

internal class PdfFileModel(long id, string filePath, PdfFileOperator fileOperator) : BaseFileModel(id, filePath, fileOperator, MediaType.Pdf);