using MediaDeck.FileTypes.Base.Models;
using MediaDeck.FileTypes.Base.ViewModels;
using MediaDeck.FileTypes.Image.Models;

namespace MediaDeck.FileTypes.Image.ViewModels;

[Inject(InjectServiceLifetime.Transient)]
internal class ImageThumbnailPickerViewModel(BaseThumbnailPickerModel thumbnailPickerModel, ImageFileOperator imageFileOperator) : BaseThumbnailPickerViewModel(thumbnailPickerModel) {
	private readonly ImageFileOperator _imageFileOperator = imageFileOperator;

	public override void RecreateThumbnail() {
		if (this.targetFileViewModel is null) {
			return;
		}
		this.CandidateThumbnail.Value = this._imageFileOperator.CreateThumbnail(this.targetFileViewModel.FileModel, 300, 300);
	}
}