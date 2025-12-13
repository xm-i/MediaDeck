using MediaDeck.FileTypes.Base.ViewModels;
using MediaDeck.FileTypes.Image.Models;
using MediaDeck.Models.FileDetailManagers;

namespace MediaDeck.FileTypes.Image.ViewModels;

[Inject(InjectServiceLifetime.Transient)]
public class ImageThumbnailPickerViewModel(ThumbnailsManager thumbnailsManager, ImageFileOperator imageFileOperator) : BaseThumbnailPickerViewModel(thumbnailsManager) {
	private readonly ImageFileOperator _imageFileOperator = imageFileOperator;

	public override void RecreateThumbnail() {
		if (this.targetFileViewModel is null) {
			return;
		}
		this.CandidateThumbnail.Value = this._imageFileOperator.CreateThumbnail(this.targetFileViewModel.FileModel, 300, 300);
	}
}
