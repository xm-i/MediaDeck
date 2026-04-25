using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Views;
using MediaDeck.MediaItemTypes.Base.Views;

namespace MediaDeck.MediaItemTypes.FolderGroup.Views;

internal sealed partial class FolderGroupDetailViewerPreviewControlView : FolderGroupDetailViewerPreviewControlViewUserControl, IDetailViewerPreviewControlView {
	internal FolderGroupDetailViewerPreviewControlView() {
		this.InitializeComponent();
	}
}

internal class FolderGroupDetailViewerPreviewControlViewUserControl : UserControlBase<IDetailViewerViewModel> {
}