using MediaDeck.Composition.Interfaces.FileTypes.ViewModels;
using MediaDeck.Composition.Interfaces.FileTypes.Views;
using MediaDeck.FileTypes.Base.Views;

namespace MediaDeck.FileTypes.Archive.Views;

internal sealed partial class ArchiveDetailViewerPreviewControlView : ArchiveDetailViewerPreviewControlViewUserControl, IDetailViewerPreviewControlView {
	internal ArchiveDetailViewerPreviewControlView() {
		this.InitializeComponent();
	}
}

internal class ArchiveDetailViewerPreviewControlViewUserControl : UserControlBase<IDetailViewerViewModel> {
}