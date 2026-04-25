using MediaDeck.MediaItemTypes.Base.Models;
using MediaDeck.MediaItemTypes.Base.ViewModels;

namespace MediaDeck.MediaItemTypes.FolderGroup.ViewModels;

[Inject(InjectServiceLifetime.Transient)]
internal class FolderGroupThumbnailPickerViewModel : BaseThumbnailPickerViewModel {
	public FolderGroupThumbnailPickerViewModel(BaseThumbnailPickerModel thumbnailPickerModel) : base(thumbnailPickerModel) { }

	public override void RecreateThumbnail() {
		throw new NotSupportedException("FolderGroup type does not support thumbnail creation.");
	}
}