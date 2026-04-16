using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.FileTypes.Base.Models;

namespace MediaDeck.FileTypes.Pdf.Models;

internal class PdfFileModel(long id, string filePath, PdfFileOperator fileOperator, ConfigModel config) : BaseFileModel(id, filePath, fileOperator, MediaType.Pdf, config);