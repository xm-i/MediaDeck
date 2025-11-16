using MediaDeck.Composition.Bases;
using MediaDeck.FileTypes.Archive.ViewModels;
using MediaDeck.FileTypes.Base.Views.Interfaces;

namespace MediaDeck.FileTypes.Archive.Views;
public sealed partial class ArchiveThumbnailPickerView : ArchiveThumbnailPickerViewUserControl, IThumbnailPickerView {
	public ArchiveThumbnailPickerView() {
		this.InitializeComponent();
	}
}

public class ArchiveThumbnailPickerViewUserControl : UserControlBase<ArchiveThumbnailPickerViewModel> {
}
