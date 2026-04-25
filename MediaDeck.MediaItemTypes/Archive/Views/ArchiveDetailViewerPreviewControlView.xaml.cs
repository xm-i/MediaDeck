using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Views;
using MediaDeck.MediaItemTypes.Base.Views;

namespace MediaDeck.MediaItemTypes.Archive.Views;

internal sealed partial class ArchiveDetailViewerPreviewControlView : ArchiveDetailViewerPreviewControlViewUserControl, IDetailViewerPreviewControlView {
	internal ArchiveDetailViewerPreviewControlView() {
		this.InitializeComponent();
	}
}

internal class ArchiveDetailViewerPreviewControlViewUserControl : UserControlBase<IDetailViewerViewModel> {
}