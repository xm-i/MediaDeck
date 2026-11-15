using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.MediaItemTypes.UI.Base.Views;

namespace MediaDeck.MediaItemTypes.UI.Unknown.Views;

public sealed partial class UnknownDetailViewerPreviewControlView : UnknownDetailViewerPreviewControlViewUserControl {
	public UnknownDetailViewerPreviewControlView() {
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

public class UnknownDetailViewerPreviewControlViewUserControl : UserControlBase<IMediaItemViewModel> {
}