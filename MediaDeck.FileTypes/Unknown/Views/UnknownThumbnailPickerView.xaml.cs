using MediaDeck.Composition.Interfaces.FileTypes.Views;
using MediaDeck.FileTypes.Base.Views;
using MediaDeck.FileTypes.Unknown.ViewModels;

namespace MediaDeck.FileTypes.Unknown.Views;

internal sealed partial class UnknownThumbnailPickerView : UnknownThumbnailPickerViewUserControl, IThumbnailPickerView {
	internal UnknownThumbnailPickerView() {
		this.InitializeComponent();
	}
}

internal class UnknownThumbnailPickerViewUserControl : UserControlBase<UnknownThumbnailPickerViewModel> { }