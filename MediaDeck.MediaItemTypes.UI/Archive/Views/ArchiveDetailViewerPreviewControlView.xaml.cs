using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Views;
using MediaDeck.MediaItemTypes.UI.Base.Views;

namespace MediaDeck.MediaItemTypes.UI.Archive.Views;

public sealed partial class ArchiveDetailViewerPreviewControlView : ArchiveDetailViewerPreviewControlViewUserControl, IDetailViewerPreviewControlView {
	public ArchiveDetailViewerPreviewControlView() {
		this.InitializeComponent();
	}
}

public class ArchiveDetailViewerPreviewControlViewUserControl : UserControlBase<IMediaItemViewModel> {
}