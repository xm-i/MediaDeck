using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.MediaItemTypes.UI.Base.Views;

namespace MediaDeck.MediaItemTypes.UI.Unknown.Views;

public sealed partial class UnknownDetailViewerPreviewControlView : UnknownDetailViewerPreviewControlViewUserControl {
	public UnknownDetailViewerPreviewControlView() {
		this.InitializeComponent();
	}
}

public class UnknownDetailViewerPreviewControlViewUserControl : UserControlBase<IMediaItemViewModel> {
}