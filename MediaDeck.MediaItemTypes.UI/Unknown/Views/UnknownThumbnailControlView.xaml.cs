using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.MediaItemTypes.UI.Base.Views;

namespace MediaDeck.MediaItemTypes.UI.Unknown.Views;

public sealed partial class UnknownThumbnailControlView : UnknownThumbnailControlViewUserControl {
	public UnknownThumbnailControlView() {
		this.InitializeComponent();
	}
}

public class UnknownThumbnailControlViewUserControl : UserControlBase<IMediaItemViewModel> {
}