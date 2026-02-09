using MediaDeck.FileTypes.Base.ViewModels;
using MediaDeck.Models.FileDetailManagers;

namespace MediaDeck.FileTypes.Unknown.ViewModels;

[Inject(InjectServiceLifetime.Transient)]
public class UnknownThumbnailPickerViewModel : BaseThumbnailPickerViewModel {
	public UnknownThumbnailPickerViewModel(ThumbnailsManager thumbnailsManager) : base(thumbnailsManager) {
	}

	public override void RecreateThumbnail() {
		throw new NotSupportedException("Unknown file type does not support thumbnail creation.");
	}
}
