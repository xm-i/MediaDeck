using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.MediaItemTypes.UI.Base.Views;

namespace MediaDeck.MediaItemTypes.UI.Image.Views;

public sealed partial class ImageThumbnailControlView : ImageThumbnailControlViewUserControl {
	public ImageThumbnailControlView() {
		this.InitializeComponent();
	}
}

public class ImageThumbnailControlViewUserControl : UserControlBase<IMediaItemViewModel> {
}