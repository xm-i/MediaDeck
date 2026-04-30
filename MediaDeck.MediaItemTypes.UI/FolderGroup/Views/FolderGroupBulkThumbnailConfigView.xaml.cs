using MediaDeck.MediaItemTypes.FolderGroup.ViewModels;
using MediaDeck.MediaItemTypes.UI.Base.Views;

namespace MediaDeck.MediaItemTypes.UI.FolderGroup.Views;

public sealed partial class FolderGroupBulkThumbnailConfigView : FolderGroupBulkThumbnailConfigViewUserControl {
	public FolderGroupBulkThumbnailConfigView() {
		this.InitializeComponent();
	}
}

public class FolderGroupBulkThumbnailConfigViewUserControl : UserControlBase<FolderGroupBulkThumbnailConfigViewModel> {
}