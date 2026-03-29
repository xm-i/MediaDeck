using MediaDeck.Composition.Interfaces.FileTypes.ViewModels;
using MediaDeck.Composition.Interfaces.FileTypes.Views;
using MediaDeck.FileTypes.Base.Views;

namespace MediaDeck.FileTypes.Unknown.Views;

public sealed partial class UnknownDetailViewerPreviewControlView : UnknownDetailViewerPreviewControlViewUserControl, IDetailViewerPreviewControlView {
	public UnknownDetailViewerPreviewControlView() {
		this.InitializeComponent();
	}
}

public class UnknownDetailViewerPreviewControlViewUserControl : UserControlBase<IDetailViewerViewModel> { }