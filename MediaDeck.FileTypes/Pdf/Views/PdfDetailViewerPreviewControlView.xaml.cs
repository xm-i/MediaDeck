using MediaDeck.Composition.Interfaces.FileTypes.ViewModels;
using MediaDeck.Composition.Interfaces.FileTypes.Views;
using MediaDeck.FileTypes.Base.Views;

namespace MediaDeck.FileTypes.Pdf.Views;

internal sealed partial class PdfDetailViewerPreviewControlView : PdfDetailViewerPreviewControlViewUserControl, IDetailViewerPreviewControlView {
	internal PdfDetailViewerPreviewControlView() {
		this.InitializeComponent();
	}
}

internal class PdfDetailViewerPreviewControlViewUserControl : UserControlBase<IDetailViewerViewModel> {
}