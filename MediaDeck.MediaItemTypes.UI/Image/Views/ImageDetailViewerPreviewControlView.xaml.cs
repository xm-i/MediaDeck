using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.MediaItemTypes.UI.Base.Views;

namespace MediaDeck.MediaItemTypes.UI.Image.Views;

public sealed partial class ImageDetailViewerPreviewControlView : ImageDetailViewerPreviewControlViewUserControl {
	public ImageDetailViewerPreviewControlView() {
		this.InitializeComponent();
	}

	/// <inheritdoc/>
	protected override void OnViewModelChanged(IMediaItemViewModel? oldViewModel, IMediaItemViewModel? newViewModel) {
		base.OnViewModelChanged(oldViewModel, newViewModel);
		if (newViewModel == null) {
			this.PreviewImage.Source = null;
		}
		this.Bindings.Update();
	}
}

public class ImageDetailViewerPreviewControlViewUserControl : UserControlBase<IMediaItemViewModel> {
}