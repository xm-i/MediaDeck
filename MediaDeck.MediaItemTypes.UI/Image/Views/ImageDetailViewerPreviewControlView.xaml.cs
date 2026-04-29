using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.MediaItemTypes.UI.Base.Views;

namespace MediaDeck.MediaItemTypes.UI.Image.Views;

public sealed partial class ImageDetailViewerPreviewControlView : ImageDetailViewerPreviewControlViewUserControl {
	public ImageDetailViewerPreviewControlView() {
		this.InitializeComponent();
	}
}

public class ImageDetailViewerPreviewControlViewUserControl : UserControlBase<IMediaItemViewModel> {
}