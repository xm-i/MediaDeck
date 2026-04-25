using MediaDeck.Composition.Interfaces.MediaItemTypes.Views;
using MediaDeck.MediaItemTypes.Archive.ViewModels;
using MediaDeck.MediaItemTypes.Base.Views;

namespace MediaDeck.MediaItemTypes.Archive.Views;

internal sealed partial class ArchiveThumbnailPickerView : ArchiveThumbnailPickerViewUserControl, IThumbnailPickerView {
	internal ArchiveThumbnailPickerView() {
		this.InitializeComponent();
	}
}

internal class ArchiveThumbnailPickerViewUserControl : UserControlBase<ArchiveThumbnailPickerViewModel> {
}