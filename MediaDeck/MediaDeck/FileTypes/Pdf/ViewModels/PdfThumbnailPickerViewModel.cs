using MediaDeck.FileTypes.Base.ViewModels;
using MediaDeck.FileTypes.Pdf.Models;
using MediaDeck.Models.FileDetailManagers;

namespace MediaDeck.FileTypes.Pdf.ViewModels;

[AddTransient]
public class PdfThumbnailPickerViewModel(ThumbnailsManager thumbnailsManager, PdfFileOperator pdfFileOperator) : BaseThumbnailPickerViewModel(thumbnailsManager) {
	private readonly PdfFileOperator _pdfFileOperator = pdfFileOperator;

	public BindableReactiveProperty<int> PageNumber {
		get;
	} = new(1);

	public override void RecreateThumbnail() {
		if (this.targetFileViewModel is null) {
			return;
		}
		try {
			this.CandidateThumbnail.Value = this._pdfFileOperator.CreateThumbnail(this.targetFileViewModel.FilePath, 300, 300, this.PageNumber.Value);
		} catch (Exception) {
			this.CandidateThumbnail.Value = null;
		}
	}
}
