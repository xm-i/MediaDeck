using MediaDeck.MediaItemTypes.Base.Models;
using MediaDeck.MediaItemTypes.Base.ViewModels;
using MediaDeck.MediaItemTypes.Image.Models;

namespace MediaDeck.MediaItemTypes.Image.ViewModels;

[Inject(InjectServiceLifetime.Transient)]
public class ImageThumbnailPickerViewModel(BaseThumbnailPickerModel thumbnailPickerModel, ImageMediaItemOperator ImageMediaItemOperator) : BaseThumbnailPickerViewModel<BaseThumbnailPickerModel>(thumbnailPickerModel) {
	private readonly ImageMediaItemOperator _ImageMediaItemOperator = ImageMediaItemOperator;

	public override void RecreateThumbnail() {
		if (this.targetFileViewModel is null) {
			return;
		}
		this.CandidateThumbnail.Value = this._ImageMediaItemOperator.CreateThumbnail(this.targetFileViewModel.FileModel, 300, 300);
	}
}