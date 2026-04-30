using MediaDeck.MediaItemTypes.Image.ViewModels;
using MediaDeck.MediaItemTypes.UI.Base.Views;

namespace MediaDeck.MediaItemTypes.UI.Image.Views;

public sealed partial class ImageBulkThumbnailConfigView : ImageBulkThumbnailConfigViewUserControl {
	public ImageBulkThumbnailConfigView() {
		this.InitializeComponent();
	}
}

public class ImageBulkThumbnailConfigViewUserControl : UserControlBase<ImageBulkThumbnailConfigViewModel> {
}