using MediaDeck.MediaItemTypes.FolderGroup.ViewModels;
using MediaDeck.MediaItemTypes.UI.Base.Views;

namespace MediaDeck.MediaItemTypes.UI.FolderGroup.Views;

public sealed partial class FolderGroupThumbnailPickerView : FolderGroupThumbnailPickerViewUserControl {
	public FolderGroupThumbnailPickerView() {
		this.InitializeComponent();
	}
}

public class FolderGroupThumbnailPickerViewUserControl : UserControlBase<FolderGroupThumbnailPickerViewModel> {
}