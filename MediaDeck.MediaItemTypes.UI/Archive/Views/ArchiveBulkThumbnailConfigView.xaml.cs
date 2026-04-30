using MediaDeck.MediaItemTypes.Archive.ViewModels;
using MediaDeck.MediaItemTypes.UI.Base.Views;

namespace MediaDeck.MediaItemTypes.UI.Archive.Views;

public sealed partial class ArchiveBulkThumbnailConfigView : ArchiveBulkThumbnailConfigViewUserControl {
	public ArchiveBulkThumbnailConfigView() {
		this.InitializeComponent();
	}
}

public class ArchiveBulkThumbnailConfigViewUserControl : UserControlBase<ArchiveBulkThumbnailConfigViewModel> {
}