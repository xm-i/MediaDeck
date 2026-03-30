using MediaDeck.FileTypes.Base.Models;
using MediaDeck.FileTypes.Base.ViewModels;

namespace MediaDeck.FileTypes.Unknown.ViewModels;

[Inject(InjectServiceLifetime.Transient)]
internal class UnknownThumbnailPickerViewModel : BaseThumbnailPickerViewModel {
	public UnknownThumbnailPickerViewModel(BaseThumbnailPickerModel thumbnailPickerModel) : base(thumbnailPickerModel) { }

	public override void RecreateThumbnail() {
		throw new NotSupportedException("Unknown file type does not support thumbnail creation.");
	}
}