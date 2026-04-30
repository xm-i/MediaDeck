using MediaDeck.MediaItemTypes.Pdf.ViewModels;
using MediaDeck.MediaItemTypes.UI.Base.Views;

namespace MediaDeck.MediaItemTypes.UI.Pdf.Views;

public sealed partial class PdfBulkThumbnailConfigView : PdfBulkThumbnailConfigViewUserControl {
	public PdfBulkThumbnailConfigView() {
		this.InitializeComponent();
	}
}

public class PdfBulkThumbnailConfigViewUserControl : UserControlBase<PdfBulkThumbnailConfigViewModel> {
}