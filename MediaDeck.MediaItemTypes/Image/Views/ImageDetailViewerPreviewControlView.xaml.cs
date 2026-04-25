using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Views;
using MediaDeck.MediaItemTypes.Base.Views;

namespace MediaDeck.MediaItemTypes.Image.Views;

internal sealed partial class ImageDetailViewerPreviewControlView : ImageDetailViewerPreviewControlViewUserControl, IDetailViewerPreviewControlView {
	internal ImageDetailViewerPreviewControlView() {
		this.InitializeComponent();
	}
}

internal class ImageDetailViewerPreviewControlViewUserControl : UserControlBase<IDetailViewerViewModel> {
}