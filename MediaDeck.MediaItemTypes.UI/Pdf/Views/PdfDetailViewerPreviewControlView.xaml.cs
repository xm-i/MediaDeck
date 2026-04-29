using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.MediaItemTypes.UI.Base.Views;

namespace MediaDeck.MediaItemTypes.UI.Pdf.Views;

public sealed partial class PdfDetailViewerPreviewControlView : PdfDetailViewerPreviewControlViewUserControl {
	public PdfDetailViewerPreviewControlView() {
		this.InitializeComponent();
	}
}

public class PdfDetailViewerPreviewControlViewUserControl : UserControlBase<IMediaItemViewModel> {
}