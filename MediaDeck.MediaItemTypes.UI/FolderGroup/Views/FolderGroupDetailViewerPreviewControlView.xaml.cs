using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Views;
using MediaDeck.MediaItemTypes.UI.Base.Views;

namespace MediaDeck.MediaItemTypes.UI.FolderGroup.Views;

public sealed partial class FolderGroupDetailViewerPreviewControlView : FolderGroupDetailViewerPreviewControlViewUserControl, IDetailViewerPreviewControlView {
	public FolderGroupDetailViewerPreviewControlView() {
		this.InitializeComponent();
	}
}

public class FolderGroupDetailViewerPreviewControlViewUserControl : UserControlBase<IMediaItemViewModel> {
}