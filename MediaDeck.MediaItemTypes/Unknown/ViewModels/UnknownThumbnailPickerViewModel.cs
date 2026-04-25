using MediaDeck.MediaItemTypes.Base.Models;
using MediaDeck.MediaItemTypes.Base.ViewModels;

namespace MediaDeck.MediaItemTypes.Unknown.ViewModels;

[Inject(InjectServiceLifetime.Transient)]
internal class UnknownThumbnailPickerViewModel : BaseThumbnailPickerViewModel {
	public UnknownThumbnailPickerViewModel(BaseThumbnailPickerModel thumbnailPickerModel) : base(thumbnailPickerModel) { }

	public override void RecreateThumbnail() {
		throw new NotSupportedException("Unknown file type does not support thumbnail creation.");
	}
}