using MediaDeck.Composition.Interfaces.FileTypes.Views;
using MediaDeck.FileTypes.Base.Views;
using MediaDeck.FileTypes.FolderGroup.ViewModels;

namespace MediaDeck.FileTypes.FolderGroup.Views;

internal sealed partial class FolderGroupThumbnailPickerView : FolderGroupThumbnailPickerViewUserControl, IThumbnailPickerView {
	internal FolderGroupThumbnailPickerView() {
		this.InitializeComponent();
	}
}

internal class FolderGroupThumbnailPickerViewUserControl : UserControlBase<FolderGroupThumbnailPickerViewModel> {
}
