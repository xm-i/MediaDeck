using MediaDeck.Composition.Bases;
using MediaDeck.FileTypes.Base.Views.Interfaces;
using MediaDeck.FileTypes.Image.ViewModels;

namespace MediaDeck.FileTypes.Image.Views;
public sealed partial class ImageThumbnailPickerView : ImageThumbnailPickerViewUserControl, IThumbnailPickerView {
	public ImageThumbnailPickerView() {
		this.InitializeComponent();
	}
}

public class ImageThumbnailPickerViewUserControl : UserControlBase<ImageThumbnailPickerViewModel> {
}
