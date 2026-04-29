using MediaDeck.MediaItemTypes.UI.Base.Views;
using MediaDeck.MediaItemTypes.Unknown.ViewModels;

namespace MediaDeck.MediaItemTypes.UI.Unknown.Views;

public sealed partial class UnknownThumbnailPickerView : UnknownThumbnailPickerViewUserControl {
	public UnknownThumbnailPickerView() {
		this.InitializeComponent();
	}
}

public class UnknownThumbnailPickerViewUserControl : UserControlBase<UnknownThumbnailPickerViewModel> {
}