using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Views;
using MediaDeck.MediaItemTypes.UI.Base.Views;

namespace MediaDeck.MediaItemTypes.UI.Unknown.Views;

public sealed partial class UnknownThumbnailControlView : UnknownThumbnailControlViewUserControl, IThumbnailControlView {
	public UnknownThumbnailControlView() {
		this.InitializeComponent();
	}
}

public class UnknownThumbnailControlViewUserControl : UserControlBase<IMediaItemViewModel> {
}