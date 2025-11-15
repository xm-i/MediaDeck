using MediaDeck.Composition.Bases;
using MediaDeck.FileTypes.Base.Views;
using MediaDeck.ViewModels.Panes.ViewerPanes;

namespace MediaDeck.FileTypes.Video.Views;
public sealed partial class VideoDetailViewerPreviewControlView : VideoDetailViewerPreviewControlViewUserControl, IDetailViewerPreviewControlView {
	public VideoDetailViewerPreviewControlView() {
		this.InitializeComponent();
	}
}

public class VideoDetailViewerPreviewControlViewUserControl : UserControlBase<DetailViewerViewModel> {
}

