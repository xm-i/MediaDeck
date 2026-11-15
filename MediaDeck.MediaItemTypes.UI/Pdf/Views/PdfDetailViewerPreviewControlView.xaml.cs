using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.MediaItemTypes.UI.Base.Views;

namespace MediaDeck.MediaItemTypes.UI.Pdf.Views;

public sealed partial class PdfDetailViewerPreviewControlView : PdfDetailViewerPreviewControlViewUserControl {
	public PdfDetailViewerPreviewControlView() {
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

public class PdfDetailViewerPreviewControlViewUserControl : UserControlBase<IMediaItemViewModel> {
}