using MediaDeck.Composition.Bases;
using MediaDeck.FileTypes.Base.Views;
using MediaDeck.FileTypes.Archive.ViewModels;

namespace MediaDeck.FileTypes.Archive.Views;
public sealed partial class ArchiveThumbnailPickerView : ArchiveThumbnailPickerViewUserControl, IThumbnailPickerView {
	public ArchiveThumbnailPickerView() {
		this.InitializeComponent();
	}
}

public class ArchiveThumbnailPickerViewUserControl : UserControlBase<ArchiveThumbnailPickerViewModel> {
}
