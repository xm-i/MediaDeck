using MediaDeck.Composition.Interfaces.FileTypes.Views;
using MediaDeck.FileTypes.Archive.ViewModels;
using MediaDeck.FileTypes.Base.Views;

namespace MediaDeck.FileTypes.Archive.Views;

internal sealed partial class ArchiveThumbnailPickerView : ArchiveThumbnailPickerViewUserControl, IThumbnailPickerView {
	internal ArchiveThumbnailPickerView() {
		this.InitializeComponent();
	}
}

internal class ArchiveThumbnailPickerViewUserControl : UserControlBase<ArchiveThumbnailPickerViewModel> { }