using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Views;
using MediaDeck.MediaItemTypes.UI.Base.Views;

namespace MediaDeck.MediaItemTypes.UI.FolderGroup.Views;

public sealed partial class FolderGroupThumbnailControlView : FolderGroupThumbnailControlViewUserControl, IThumbnailControlView {
	public FolderGroupThumbnailControlView() {
		this.InitializeComponent();
	}
}

public class FolderGroupThumbnailControlViewUserControl : UserControlBase<IMediaItemViewModel> {
}