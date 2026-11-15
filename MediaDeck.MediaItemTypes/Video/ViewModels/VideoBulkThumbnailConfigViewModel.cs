using System.Threading.Tasks;
using FFMpegCore;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Composition.Stores.Config.Model;
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

	public VideoBulkThumbnailConfigViewModel(VideoMediaItemOperator videoOperator, BaseThumbnailPickerModel thumbnailPickerModel, ConfigModel config)
		: base(MediaType.Video, thumbnailPickerModel, config) {
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

	protected override async Task<byte[]?> GenerateThumbnailAsync(IMediaItemViewModel target) {
		var time = await this.ResolveTimeAsync(target.FilePath).ConfigureAwait(false);
		return await this._videoOperator.CreateThumbnailAsync(target.FileModel, this._config.ThumbnailConfig.ThumbnailSize.Value, this._config.ThumbnailConfig.ThumbnailSize.Value, time).ConfigureAwait(false);
	}

	private async Task<TimeSpan> ResolveTimeAsync(string filePath) {
		if (this.Mode.Value == VideoBulkThumbnailMode.Seconds) {
			return TimeSpan.FromSeconds(Math.Max(0, this.Seconds.Value));
		}
		var metadata = await FFProbe.AnalyseAsync(filePath).ConfigureAwait(false);
		var pct = Math.Clamp(this.Percentage.Value, 0, 100) / 100.0;
		return TimeSpan.FromSeconds(metadata.Duration.TotalSeconds * pct);
	}
}