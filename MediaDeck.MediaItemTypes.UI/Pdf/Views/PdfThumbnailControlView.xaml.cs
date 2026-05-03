using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.MediaItemTypes.Pdf.ViewModels;
using MediaDeck.MediaItemTypes.UI.Base.Views;

namespace MediaDeck.MediaItemTypes.UI.Pdf.Views;

public sealed partial class PdfThumbnailControlView : PdfThumbnailControlViewUserControl {
	public PdfThumbnailControlView() {
		this.InitializeComponent();
	}

	public PdfMediaItemViewModel? CastedViewModel {
		get {
			return this.ViewModel as PdfMediaItemViewModel;
		}
	}

	protected override void OnViewModelChanged(IMediaItemViewModel? oldViewModel, IMediaItemViewModel? newViewModel) {
		base.OnViewModelChanged(oldViewModel, newViewModel);
		this.Bindings.Update();
	}
}

public class PdfThumbnailControlViewUserControl : UserControlBase<IMediaItemViewModel> {
}