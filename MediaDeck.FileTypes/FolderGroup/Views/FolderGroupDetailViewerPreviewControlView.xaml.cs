using MediaDeck.Composition.Interfaces.FileTypes.ViewModels;
using MediaDeck.Composition.Interfaces.FileTypes.Views;
using MediaDeck.FileTypes.Base.Views;

namespace MediaDeck.FileTypes.FolderGroup.Views;

internal sealed partial class FolderGroupDetailViewerPreviewControlView : FolderGroupDetailViewerPreviewControlViewUserControl, IDetailViewerPreviewControlView {
	internal FolderGroupDetailViewerPreviewControlView() {
		this.InitializeComponent();
	}
}

internal class FolderGroupDetailViewerPreviewControlViewUserControl : UserControlBase<IDetailViewerViewModel> {
}
