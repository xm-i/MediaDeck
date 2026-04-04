using MediaDeck.Composition.Interfaces.FileTypes.ViewModels;
using MediaDeck.Composition.Interfaces.FileTypes.Views;
using MediaDeck.FileTypes.Base.Views;

namespace MediaDeck.FileTypes.Unknown.Views;

internal sealed partial class UnknownDetailViewerPreviewControlView : UnknownDetailViewerPreviewControlViewUserControl, IDetailViewerPreviewControlView {
	internal UnknownDetailViewerPreviewControlView() {
		this.InitializeComponent();
	}
}

internal class UnknownDetailViewerPreviewControlViewUserControl : UserControlBase<IDetailViewerViewModel> {
}