using MediaDeck.Composition.Interfaces.FileTypes.Views;
using MediaDeck.FileTypes.Archive.ViewModels;
using MediaDeck.FileTypes.Base.Views;

namespace MediaDeck.FileTypes.Archive.Views;

public sealed partial class ArchiveThumbnailPickerView : ArchiveThumbnailPickerViewUserControl, IThumbnailPickerView {
	public ArchiveThumbnailPickerView() {
		this.InitializeComponent();
	}
}

public class ArchiveThumbnailPickerViewUserControl : UserControlBase<ArchiveThumbnailPickerViewModel> { }