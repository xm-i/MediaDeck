using MediaDeck.Composition.Bases;
using MediaDeck.FileTypes.Base.Views;
using MediaDeck.ViewModels.Panes.ViewerPanes;

namespace MediaDeck.FileTypes.Archive.Views;
public sealed partial class ArchiveDetailViewerPreviewControlView : ArchiveDetailViewerPreviewControlViewUserControl, IDetailViewerPreviewControlView {
	public ArchiveDetailViewerPreviewControlView() {
		this.InitializeComponent();
	}
}

public class ArchiveDetailViewerPreviewControlViewUserControl : UserControlBase<DetailViewerViewModel> {
}

