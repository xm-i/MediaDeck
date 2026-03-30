using MediaDeck.Composition.Interfaces.FileTypes.ViewModels;
using MediaDeck.Composition.Interfaces.FileTypes.Views;
using MediaDeck.FileTypes.Base.Views;

namespace MediaDeck.FileTypes.Video.Views;

internal sealed partial class VideoDetailViewerPreviewControlView : VideoDetailViewerPreviewControlViewUserControl, IDetailViewerPreviewControlView {
	internal VideoDetailViewerPreviewControlView() {
		this.InitializeComponent();
	}
}

internal class VideoDetailViewerPreviewControlViewUserControl : UserControlBase<IDetailViewerViewModel> { }