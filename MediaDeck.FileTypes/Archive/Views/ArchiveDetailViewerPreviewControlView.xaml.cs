using MediaDeck.Composition.Interfaces.FileTypes.ViewModels;
using MediaDeck.Composition.Interfaces.FileTypes.Views;
using MediaDeck.FileTypes.Base.Views;

namespace MediaDeck.FileTypes.Archive.Views;

public sealed partial class ArchiveDetailViewerPreviewControlView : ArchiveDetailViewerPreviewControlViewUserControl, IDetailViewerPreviewControlView {
	public ArchiveDetailViewerPreviewControlView() {
		this.InitializeComponent();
	}
}

public class ArchiveDetailViewerPreviewControlViewUserControl : UserControlBase<IDetailViewerViewModel> { }