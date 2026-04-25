using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Views;
using MediaDeck.MediaItemTypes.Base.Views;

namespace MediaDeck.MediaItemTypes.Unknown.Views;

internal sealed partial class UnknownDetailViewerPreviewControlView : UnknownDetailViewerPreviewControlViewUserControl, IDetailViewerPreviewControlView {
	internal UnknownDetailViewerPreviewControlView() {
		this.InitializeComponent();
	}
}

internal class UnknownDetailViewerPreviewControlViewUserControl : UserControlBase<IDetailViewerViewModel> {
}