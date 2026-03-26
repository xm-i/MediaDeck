using MediaDeck.Composition.Bases;
using MediaDeck.Composition.Interfaces.FileTypes.Views;
using MediaDeck.ViewModels.Panes.ViewerPanes;

namespace MediaDeck.FileTypes.Image.Views;
public sealed partial class ImageDetailViewerPreviewControlView : ImageDetailViewerPreviewControlViewUserControl, IDetailViewerPreviewControlView {
	public ImageDetailViewerPreviewControlView() {
		this.InitializeComponent();
	}
}

public class ImageDetailViewerPreviewControlViewUserControl : UserControlBase<DetailViewerViewModel> {
}

