using MediaDeck.MediaItemTypes.UI.Base.Views;
using MediaDeck.MediaItemTypes.Unknown.ViewModels;

namespace MediaDeck.MediaItemTypes.UI.Unknown.Views;

public sealed partial class UnknownBulkThumbnailConfigView : UnknownBulkThumbnailConfigViewUserControl {
	public UnknownBulkThumbnailConfigView() {
		this.InitializeComponent();
	}
}

public class UnknownBulkThumbnailConfigViewUserControl : UserControlBase<UnknownBulkThumbnailConfigViewModel> {
}