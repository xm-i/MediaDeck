using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.MediaItemTypes.Base.Models;

namespace MediaDeck.MediaItemTypes.Pdf.Models;

internal class PdfMediaItemModel(long id, string filePath, PdfMediaItemOperator fileOperator, ConfigModel config) : BaseMediaItemModel(id, filePath, fileOperator, MediaType.Pdf, config);