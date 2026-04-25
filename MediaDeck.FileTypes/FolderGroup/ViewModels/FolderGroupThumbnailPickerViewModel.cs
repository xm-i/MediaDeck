using MediaDeck.FileTypes.Base.Models;
using MediaDeck.FileTypes.Base.ViewModels;

namespace MediaDeck.FileTypes.FolderGroup.ViewModels;

[Inject(InjectServiceLifetime.Transient)]
internal class FolderGroupThumbnailPickerViewModel : BaseThumbnailPickerViewModel {
	public FolderGroupThumbnailPickerViewModel(BaseThumbnailPickerModel thumbnailPickerModel) : base(thumbnailPickerModel) { }

	public override void RecreateThumbnail() {
		throw new NotSupportedException("FolderGroup type does not support thumbnail creation.");
	}
}
