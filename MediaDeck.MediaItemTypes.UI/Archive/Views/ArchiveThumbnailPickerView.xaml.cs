using MediaDeck.MediaItemTypes.Archive.ViewModels;
using MediaDeck.MediaItemTypes.UI.Base.Views;

namespace MediaDeck.MediaItemTypes.UI.Archive.Views;

public sealed partial class ArchiveThumbnailPickerView : ArchiveThumbnailPickerViewUserControl {
	public ArchiveThumbnailPickerView() {
		this.InitializeComponent();
	}
}

public class ArchiveThumbnailPickerViewUserControl : UserControlBase<ArchiveThumbnailPickerViewModel> {
}