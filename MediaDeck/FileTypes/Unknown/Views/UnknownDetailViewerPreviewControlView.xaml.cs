using MediaDeck.Composition.Bases;
using MediaDeck.FileTypes.Base.Views.Interfaces;
using MediaDeck.ViewModels.Panes.ViewerPanes;

namespace MediaDeck.FileTypes.Unknown.Views;
public sealed partial class UnknownDetailViewerPreviewControlView : UnknownDetailViewerPreviewControlViewUserControl, IDetailViewerPreviewControlView {
	public UnknownDetailViewerPreviewControlView() {
		this.InitializeComponent();
	}
}

public class UnknownDetailViewerPreviewControlViewUserControl : UserControlBase<DetailViewerViewModel> {
}
