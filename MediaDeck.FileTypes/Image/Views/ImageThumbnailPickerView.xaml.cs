using MediaDeck.Composition.Interfaces.FileTypes.Views;
using MediaDeck.FileTypes.Base.Views;
using MediaDeck.FileTypes.Image.ViewModels;

namespace MediaDeck.FileTypes.Image.Views;

internal sealed partial class ImageThumbnailPickerView : ImageThumbnailPickerViewUserControl, IThumbnailPickerView {
	internal ImageThumbnailPickerView() {
		this.InitializeComponent();
	}
}

internal class ImageThumbnailPickerViewUserControl : UserControlBase<ImageThumbnailPickerViewModel> {
}