using MediaDeck.Composition.Bases;
using MediaDeck.Composition.Interfaces.FileTypes.Views;
using MediaDeck.ViewModels.Panes.ViewerPanes;

namespace MediaDeck.FileTypes.Pdf.Views;
public sealed partial class PdfDetailViewerPreviewControlView : PdfDetailViewerPreviewControlViewUserControl, IDetailViewerPreviewControlView {
	public PdfDetailViewerPreviewControlView() {
		this.InitializeComponent();
	}
}

public class PdfDetailViewerPreviewControlViewUserControl : UserControlBase<DetailViewerViewModel> {
}

