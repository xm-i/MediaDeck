using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;
using MediaDeck.MediaItemTypes.Archive.Models;
using MediaDeck.MediaItemTypes.Base.ViewModels;

namespace MediaDeck.MediaItemTypes.Archive.ViewModels;

[Inject(InjectServiceLifetime.Transient)]
public class ArchiveMediaItemViewModel() : BaseMediaItemViewModel(MediaType.Archive) {
	public int? FileCount {
		get;
		private set;
	}

	public string FileCountText {
		get {
			return this.FileCount is { } c ? $"{c} files" : string.Empty;
		}
	}

	public override void Initialize(IMediaItemModel fileModel) {
		base.Initialize(fileModel);
		if (fileModel is ArchiveMediaItemModel archiveModel) {
			this.FileCount = archiveModel.FileCount;
		}
	}
}