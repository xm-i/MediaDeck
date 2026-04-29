using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.MediaItemTypes.UI.Base.Views;

namespace MediaDeck.MediaItemTypes.UI.FolderGroup.Views;

public sealed partial class FolderGroupThumbnailControlView : FolderGroupThumbnailControlViewUserControl {
	public FolderGroupThumbnailControlView() {
		this.InitializeComponent();
	}
}

public class FolderGroupThumbnailControlViewUserControl : UserControlBase<IMediaItemViewModel> {
}