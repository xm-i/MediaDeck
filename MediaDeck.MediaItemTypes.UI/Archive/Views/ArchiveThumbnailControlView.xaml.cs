using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Views;
using MediaDeck.MediaItemTypes.UI.Base.Views;

namespace MediaDeck.MediaItemTypes.UI.Archive.Views;

public sealed partial class ArchiveThumbnailControlView : ArchiveThumbnailControlViewUserControl, IThumbnailControlView {
	public ArchiveThumbnailControlView() {
		this.InitializeComponent();
	}
}

public class ArchiveThumbnailControlViewUserControl : UserControlBase<IMediaItemViewModel> {
}