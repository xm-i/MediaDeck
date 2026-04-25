using MediaDeck.Composition.Interfaces.MediaItemTypes.Views;
using MediaDeck.MediaItemTypes.Base.Views;
using MediaDeck.MediaItemTypes.Unknown.ViewModels;

namespace MediaDeck.MediaItemTypes.Unknown.Views;

internal sealed partial class UnknownThumbnailPickerView : UnknownThumbnailPickerViewUserControl, IThumbnailPickerView {
	internal UnknownThumbnailPickerView() {
		this.InitializeComponent();
	}
}

internal class UnknownThumbnailPickerViewUserControl : UserControlBase<UnknownThumbnailPickerViewModel> {
}