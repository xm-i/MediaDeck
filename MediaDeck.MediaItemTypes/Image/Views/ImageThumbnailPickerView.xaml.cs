using MediaDeck.Composition.Interfaces.MediaItemTypes.Views;
using MediaDeck.MediaItemTypes.Base.Views;
using MediaDeck.MediaItemTypes.Image.ViewModels;

namespace MediaDeck.MediaItemTypes.Image.Views;

internal sealed partial class ImageThumbnailPickerView : ImageThumbnailPickerViewUserControl, IThumbnailPickerView {
	internal ImageThumbnailPickerView() {
		this.InitializeComponent();
	}
}

internal class ImageThumbnailPickerViewUserControl : UserControlBase<ImageThumbnailPickerViewModel> {
}