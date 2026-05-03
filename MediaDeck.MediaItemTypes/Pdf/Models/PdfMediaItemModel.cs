using MediaDeck.Composition.Enum;
using MediaDeck.MediaItemTypes.Base.Models;

namespace MediaDeck.MediaItemTypes.Pdf.Models;

[Inject(InjectServiceLifetime.Transient)]
public class PdfMediaItemModel(PdfMediaItemOperator fileOperator, PdfMediaItemTypeProvider mediaItemTypeProvider, IServiceProvider scopedServiceProvider) : BaseMediaItemModel(fileOperator, MediaType.Pdf, mediaItemTypeProvider, scopedServiceProvider) {
	/// <summary>
	/// PDFページ数
	/// </summary>
	public int? FileCount {
		get;
		set;
	}
}