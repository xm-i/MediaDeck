using MediaDeck.Composition.Bases;
using MediaDeck.Composition.Interfaces.FileTypes.Views;
using MediaDeck.FileTypes.Pdf.ViewModels;

namespace MediaDeck.FileTypes.Pdf.Views;
public sealed partial class PdfThumbnailPickerView : PdfThumbnailPickerViewUserControl, IThumbnailPickerView {
	public PdfThumbnailPickerView() {
		this.InitializeComponent();
	}
}

public class PdfThumbnailPickerViewUserControl : UserControlBase<PdfThumbnailPickerViewModel> {
}
