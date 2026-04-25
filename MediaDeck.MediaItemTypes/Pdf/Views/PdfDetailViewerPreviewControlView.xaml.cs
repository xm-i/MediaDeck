using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Views;
using MediaDeck.MediaItemTypes.Base.Views;

namespace MediaDeck.MediaItemTypes.Pdf.Views;

internal sealed partial class PdfDetailViewerPreviewControlView : PdfDetailViewerPreviewControlViewUserControl, IDetailViewerPreviewControlView {
	internal PdfDetailViewerPreviewControlView() {
		this.InitializeComponent();
	}
}

internal class PdfDetailViewerPreviewControlViewUserControl : UserControlBase<IDetailViewerViewModel> {
}