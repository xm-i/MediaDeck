using MediaDeck.Composition.Interfaces.MediaItemTypes.Views;
using MediaDeck.MediaItemTypes.Base.Views;
using MediaDeck.MediaItemTypes.FolderGroup.ViewModels;

namespace MediaDeck.MediaItemTypes.FolderGroup.Views;

internal sealed partial class FolderGroupThumbnailPickerView : FolderGroupThumbnailPickerViewUserControl, IThumbnailPickerView {
	internal FolderGroupThumbnailPickerView() {
		this.InitializeComponent();
	}
}

internal class FolderGroupThumbnailPickerViewUserControl : UserControlBase<FolderGroupThumbnailPickerViewModel> {
}