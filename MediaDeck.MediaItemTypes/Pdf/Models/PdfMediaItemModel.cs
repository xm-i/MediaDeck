using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.MediaItemTypes.Base.Models;

namespace MediaDeck.MediaItemTypes.Pdf.Models;

public class PdfMediaItemModel(long id, string filePath, PdfMediaItemOperator fileOperator, IMediaItemTypeProvider mediaItemTypeProvider, IServiceProvider scopedServiceProvider) : BaseMediaItemModel(id, filePath, fileOperator, MediaType.Pdf, mediaItemTypeProvider, scopedServiceProvider);