using MediaDeck.Composition.Bases;
using MediaDeck.Composition.Interfaces.FileTypes.ViewModels;
using MediaDeck.Composition.Interfaces.FileTypes.Views;

namespace MediaDeck.FileTypes.Image.Views;
public sealed partial class ImageDetailViewerPreviewControlView : ImageDetailViewerPreviewControlViewUserControl, IDetailViewerPreviewControlView {
	public ImageDetailViewerPreviewControlView() {
		this.InitializeComponent();
	}
}

public class ImageDetailViewerPreviewControlViewUserControl : UserControlBase<IDetailViewerViewModel> {
}

