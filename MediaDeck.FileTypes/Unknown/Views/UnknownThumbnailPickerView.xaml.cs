using MediaDeck.FileTypes.Unknown.ViewModels;
using MediaDeck.Composition.Interfaces.FileTypes.Views;
using MediaDeck.FileTypes.Base.Views;

namespace MediaDeck.FileTypes.Unknown.Views;

public sealed partial class UnknownThumbnailPickerView : UnknownThumbnailPickerViewUserControl, IThumbnailPickerView {
	public UnknownThumbnailPickerView() {
		this.InitializeComponent();
	}
}

public class UnknownThumbnailPickerViewUserControl : UserControlBase<UnknownThumbnailPickerViewModel> {
}
