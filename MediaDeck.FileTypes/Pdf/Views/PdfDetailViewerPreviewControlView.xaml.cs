using MediaDeck.Composition.Bases;
using MediaDeck.Composition.Interfaces.FileTypes.ViewModels;
using MediaDeck.Composition.Interfaces.FileTypes.Views;

namespace MediaDeck.FileTypes.Pdf.Views;
public sealed partial class PdfDetailViewerPreviewControlView : PdfDetailViewerPreviewControlViewUserControl, IDetailViewerPreviewControlView {
	public PdfDetailViewerPreviewControlView() {
		this.InitializeComponent();
	}
}

public class PdfDetailViewerPreviewControlViewUserControl : UserControlBase<IDetailViewerViewModel> {
}

