using MediaDeck.Composition.Interfaces.FileTypes.ViewModels;
using MediaDeck.Composition.Interfaces.FileTypes.Views;
using MediaDeck.FileTypes.Base.Views;

namespace MediaDeck.FileTypes.Video.Views;

public sealed partial class VideoDetailViewerPreviewControlView : VideoDetailViewerPreviewControlViewUserControl, IDetailViewerPreviewControlView {
	public VideoDetailViewerPreviewControlView() {
		this.InitializeComponent();
	}
}

public class VideoDetailViewerPreviewControlViewUserControl : UserControlBase<IDetailViewerViewModel> {
}

