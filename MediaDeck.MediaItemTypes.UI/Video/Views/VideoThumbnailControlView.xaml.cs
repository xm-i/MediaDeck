using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Views;
using MediaDeck.MediaItemTypes.UI.Base.Views;

namespace MediaDeck.MediaItemTypes.UI.Video.Views;

public sealed partial class VideoThumbnailControlView : VideoThumbnailControlViewUserControl, IThumbnailControlView {
	public VideoThumbnailControlView() {
		this.InitializeComponent();
	}
}

public class VideoThumbnailControlViewUserControl : UserControlBase<IMediaItemViewModel> {
}