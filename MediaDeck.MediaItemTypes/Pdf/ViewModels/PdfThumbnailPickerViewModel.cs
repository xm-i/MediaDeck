using MediaDeck.MediaItemTypes.Base.Models;
using MediaDeck.MediaItemTypes.Base.ViewModels;
using MediaDeck.MediaItemTypes.Pdf.Models;
using Microsoft.Extensions.Logging;

namespace MediaDeck.MediaItemTypes.Pdf.ViewModels;

[Inject(InjectServiceLifetime.Transient)]
internal class PdfThumbnailPickerViewModel(BaseThumbnailPickerModel thumbnailPickerModel, PdfMediaItemOperator PdfMediaItemOperator, ILogger<PdfThumbnailPickerViewModel> logger) : BaseThumbnailPickerViewModel(thumbnailPickerModel) {
	private readonly PdfMediaItemOperator _PdfMediaItemOperator = PdfMediaItemOperator;
	private readonly ILogger<PdfThumbnailPickerViewModel> _logger = logger;

	internal BindableReactiveProperty<int> PageNumber {
		get;
	} = new(1);

	public override void RecreateThumbnail() {
		if (this.targetFileViewModel is null) {
			return;
		}
		try {
			this.CandidateThumbnail.Value = this._PdfMediaItemOperator.CreateThumbnail(this.targetFileViewModel.FilePath, 300, 300, this.PageNumber.Value);
		} catch (Exception ex) {
			this._logger.LogError(ex, "Failed to recreate pdf thumbnail for file {FilePath} at page {PageNumber}", this.targetFileViewModel.FilePath, this.PageNumber.Value);
			this.CandidateThumbnail.Value = null;
		}
	}
}