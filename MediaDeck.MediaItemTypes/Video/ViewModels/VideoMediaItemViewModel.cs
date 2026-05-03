using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;
using MediaDeck.MediaItemTypes.Base.ViewModels;
using MediaDeck.MediaItemTypes.Video.Models;

namespace MediaDeck.MediaItemTypes.Video.ViewModels;

[Inject(InjectServiceLifetime.Transient)]
public class VideoMediaItemViewModel() : BaseMediaItemViewModel(MediaType.Video) {
	/// <summary>
	/// 動画の長さ（秒）
	/// </summary>
	public double? Duration {
		get;
		private set;
	}

	public override void Initialize(IMediaItemModel fileModel) {
		base.Initialize(fileModel);
		if (fileModel is VideoMediaItemModel videoModel) {
			this.Duration = videoModel.Duration;
		}
	}
}