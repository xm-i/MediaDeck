using MediaDeck.Composition.Interfaces.FileTypes.Views;
using MediaDeck.FileTypes.Base.Views;
using MediaDeck.FileTypes.Pdf.ViewModels;

namespace MediaDeck.FileTypes.Pdf.Views;

internal sealed partial class PdfThumbnailPickerView : PdfThumbnailPickerViewUserControl, IThumbnailPickerView {
	internal PdfThumbnailPickerView() {
		this.InitializeComponent();
	}
}

internal class PdfThumbnailPickerViewUserControl : UserControlBase<PdfThumbnailPickerViewModel> { }