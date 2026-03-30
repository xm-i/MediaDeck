using MediaDeck.FileTypes.Unknown.ViewModels;
using MediaDeck.Composition.Interfaces.FileTypes.Views;
using MediaDeck.FileTypes.Base.Views;

namespace MediaDeck.FileTypes.Unknown.Views;

internal sealed partial class UnknownThumbnailPickerView : UnknownThumbnailPickerViewUserControl, IThumbnailPickerView {
	internal UnknownThumbnailPickerView() {
		this.InitializeComponent();
	}
}

internal class UnknownThumbnailPickerViewUserControl : UserControlBase<UnknownThumbnailPickerViewModel> { }