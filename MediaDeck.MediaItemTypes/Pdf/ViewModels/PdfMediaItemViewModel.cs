using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;
using MediaDeck.MediaItemTypes.Base.ViewModels;
using MediaDeck.MediaItemTypes.Pdf.Models;

namespace MediaDeck.MediaItemTypes.Pdf.ViewModels;

[Inject(InjectServiceLifetime.Transient)]
public class PdfMediaItemViewModel() : BaseMediaItemViewModel(MediaType.Pdf) {
	public int? FileCount {
		get;
		private set;
	}

	public string FileCountText {
		get {
			return this.FileCount is { } c ? $"{c} files" : string.Empty;
		}
	}

	public override void Initialize(IMediaItemModel fileModel) {
		base.Initialize(fileModel);
		if (fileModel is PdfMediaItemModel pdfModel) {
			this.FileCount = pdfModel.FileCount;
		}
	}
}