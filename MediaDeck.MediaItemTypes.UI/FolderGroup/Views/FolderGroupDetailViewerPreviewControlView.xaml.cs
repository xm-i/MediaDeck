using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.MediaItemTypes.UI.Base.Views;

namespace MediaDeck.MediaItemTypes.UI.FolderGroup.Views;

public sealed partial class FolderGroupDetailViewerPreviewControlView : FolderGroupDetailViewerPreviewControlViewUserControl {
	public FolderGroupDetailViewerPreviewControlView() {
		this.InitializeComponent();
	}
}

public class FolderGroupDetailViewerPreviewControlViewUserControl : UserControlBase<IMediaItemViewModel> {
}