using MediaDeck.Composition.Interfaces.FileTypes.ViewModels;
using MediaDeck.Composition.Interfaces.FileTypes.Views;
using MediaDeck.FileTypes.Base.Views;

namespace MediaDeck.FileTypes.Image.Views;

internal sealed partial class ImageDetailViewerPreviewControlView : ImageDetailViewerPreviewControlViewUserControl, IDetailViewerPreviewControlView {
	internal ImageDetailViewerPreviewControlView() {
		this.InitializeComponent();
	}
}

internal class ImageDetailViewerPreviewControlViewUserControl : UserControlBase<IDetailViewerViewModel> { }