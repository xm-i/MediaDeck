using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.MediaItemTypes.UI.Base.Views;

namespace MediaDeck.MediaItemTypes.UI.Archive.Views;

public sealed partial class ArchiveThumbnailControlView : ArchiveThumbnailControlViewUserControl {
	public ArchiveThumbnailControlView() {
		this.InitializeComponent();
	}
}

public class ArchiveThumbnailControlViewUserControl : UserControlBase<IMediaItemViewModel> {
}