using MediaDeck.MediaItemTypes.Base.Models;
using MediaDeck.MediaItemTypes.Base.ViewModels;

namespace MediaDeck.MediaItemTypes.Unknown.ViewModels;

[Inject(InjectServiceLifetime.Transient)]
public class UnknownThumbnailPickerViewModel : BaseThumbnailPickerViewModel<BaseThumbnailPickerModel> {
	public UnknownThumbnailPickerViewModel(BaseThumbnailPickerModel thumbnailPickerModel, IFilePickerService filePickerService) : base(thumbnailPickerModel, filePickerService) { }

	public override void RecreateThumbnail() {
		throw new NotSupportedException("Unknown file type does not support thumbnail creation.");
	}
}