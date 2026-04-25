using MediaDeck.Composition.Interfaces.MediaItemTypes.Views;
using MediaDeck.MediaItemTypes.Base.Views;
using MediaDeck.MediaItemTypes.Pdf.ViewModels;

namespace MediaDeck.MediaItemTypes.Pdf.Views;

internal sealed partial class PdfThumbnailPickerView : PdfThumbnailPickerViewUserControl, IThumbnailPickerView {
	internal PdfThumbnailPickerView() {
		this.InitializeComponent();
	}
}

internal class PdfThumbnailPickerViewUserControl : UserControlBase<PdfThumbnailPickerViewModel> {
}