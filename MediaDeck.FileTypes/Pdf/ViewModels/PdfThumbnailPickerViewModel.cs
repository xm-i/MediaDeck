using MediaDeck.FileTypes.Base.Models;
using MediaDeck.FileTypes.Base.ViewModels;
using MediaDeck.FileTypes.Pdf.Models;
using Microsoft.Extensions.Logging;

namespace MediaDeck.FileTypes.Pdf.ViewModels;

[Inject(InjectServiceLifetime.Transient)]
internal class PdfThumbnailPickerViewModel(BaseThumbnailPickerModel thumbnailPickerModel, PdfFileOperator pdfFileOperator, ILogger<PdfThumbnailPickerViewModel> logger) : BaseThumbnailPickerViewModel(thumbnailPickerModel) {
	private readonly PdfFileOperator _pdfFileOperator = pdfFileOperator;
	private readonly ILogger<PdfThumbnailPickerViewModel> _logger = logger;

	internal BindableReactiveProperty<int> PageNumber {
		get;
	} = new(1);

	public override void RecreateThumbnail() {
		if (this.targetFileViewModel is null) {
			return;
		}
		try {
			this.CandidateThumbnail.Value = this._pdfFileOperator.CreateThumbnail(this.targetFileViewModel.FilePath, 300, 300, this.PageNumber.Value);
		} catch (Exception ex) {
			this._logger.LogError(ex, "Failed to recreate pdf thumbnail for file {FilePath} at page {PageNumber}", this.targetFileViewModel.FilePath, this.PageNumber.Value);
			this.CandidateThumbnail.Value = null;
		}
	}
}