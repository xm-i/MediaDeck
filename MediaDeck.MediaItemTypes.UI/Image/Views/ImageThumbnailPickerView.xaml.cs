using MediaDeck.MediaItemTypes.Image.ViewModels;
using MediaDeck.MediaItemTypes.UI.Base.Views;

namespace MediaDeck.MediaItemTypes.UI.Image.Views;

public sealed partial class ImageThumbnailPickerView : ImageThumbnailPickerViewUserControl {
	public ImageThumbnailPickerView() {
		this.InitializeComponent();
	}
}

public class ImageThumbnailPickerViewUserControl : UserControlBase<ImageThumbnailPickerViewModel> {
}