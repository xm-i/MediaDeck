using MediaDeck.Composition.Bases;
using MediaDeck.Composition.Interfaces.FileTypes.Views;
using MediaDeck.ViewModels.Panes.ViewerPanes;

namespace MediaDeck.FileTypes.Unknown.Views;
public sealed partial class UnknownDetailViewerPreviewControlView : UnknownDetailViewerPreviewControlViewUserControl, IDetailViewerPreviewControlView {
	public UnknownDetailViewerPreviewControlView() {
		this.InitializeComponent();
	}
}

public class UnknownDetailViewerPreviewControlViewUserControl : UserControlBase<DetailViewerViewModel> {
}
