using FFMpegCore;

using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.MediaItemTypes.Base.Models;
using MediaDeck.MediaItemTypes.Base.ViewModels;
using MediaDeck.MediaItemTypes.Video.Models;

namespace MediaDeck.MediaItemTypes.Video.ViewModels;

/// <summary>
/// 動画一括サムネイル抽出位置の指定方法。
/// </summary>
public enum VideoBulkThumbnailMode {
	/// <summary>
	/// 開始から N 秒地点。
	/// </summary>
	Seconds,
	/// <summary>
	/// 全体長の N% 地点。
	/// </summary>
	Percentage
}

/// <summary>
/// 動画用の一括サムネイル再生成設定ViewModel。秒指定/%指定を切り替え可能。
/// </summary>
[Inject(InjectServiceLifetime.Transient)]
public class VideoBulkThumbnailConfigViewModel : BulkThumbnailConfigViewModelBase {
	private readonly VideoMediaItemOperator _videoOperator;

	public VideoBulkThumbnailConfigViewModel(VideoMediaItemOperator videoOperator, BaseThumbnailPickerModel thumbnailPickerModel)
		: base(MediaType.Video, thumbnailPickerModel) {
		this._videoOperator = videoOperator;
	}

	/// <summary>
	/// 抽出位置の指定方法。
	/// </summary>
	public BindableReactiveProperty<VideoBulkThumbnailMode> Mode {
		get;
	} = new(VideoBulkThumbnailMode.Percentage);

	/// <summary>
	/// 開始からの秒数。
	/// </summary>
	public BindableReactiveProperty<double> Seconds {
		get;
	} = new(0);

	/// <summary>
	/// 全体長における % (0-100)。
	/// </summary>
	public BindableReactiveProperty<double> Percentage {
		get;
	} = new(20);

	protected override byte[]? GenerateThumbnail(IMediaItemViewModel target) {
		var time = this.ResolveTime(target.FilePath);
		return this._videoOperator.CreateThumbnail(target.FileModel, 300, 300, time);
	}

	private TimeSpan ResolveTime(string filePath) {
		if (this.Mode.Value == VideoBulkThumbnailMode.Seconds) {
			return TimeSpan.FromSeconds(Math.Max(0, this.Seconds.Value));
		}
		var metadata = FFProbe.Analyse(filePath);
		var pct = Math.Clamp(this.Percentage.Value, 0, 100) / 100.0;
		return TimeSpan.FromSeconds(metadata.Duration.TotalSeconds * pct);
	}
}