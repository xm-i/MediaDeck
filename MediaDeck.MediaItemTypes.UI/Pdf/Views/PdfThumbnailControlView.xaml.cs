using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Views;
using MediaDeck.MediaItemTypes.UI.Base.Views;

namespace MediaDeck.MediaItemTypes.UI.Pdf.Views;

public sealed partial class PdfThumbnailControlView : PdfThumbnailControlViewUserControl, IThumbnailControlView {
	public PdfThumbnailControlView() {
		this.InitializeComponent();
	}
}

public class PdfThumbnailControlViewUserControl : UserControlBase<IMediaItemViewModel> {
}