using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Database.Tables;
using MediaDeck.MediaItemTypes.Base;
using MediaDeck.MediaItemTypes.Video.Models;
using MediaDeck.MediaItemTypes.Video.ViewModels;
using MediaDeck.MediaItemTypes.Video.Views;

using Microsoft.Extensions.DependencyInjection;

namespace MediaDeck.MediaItemTypes.Video;

[Inject(InjectServiceLifetime.Singleton, typeof(IMediaItemType))]
internal class VideoMediaItemType : BaseMediaItemType<VideoMediaItemOperator, VideoMediaItemModel, VideoMediaItemViewModel, VideoDetailViewerPreviewControlView, VideoThumbnailPickerViewModel, VideoThumbnailPickerView> {
	private VideoDetailViewerPreviewControlView? _videoDetailViewerPreviewControlView;
	private readonly VideoMediaItemOperator _VideoMediaItemOperator;
	private readonly IServiceProvider _serviceProvider;

	public VideoMediaItemType(
		VideoMediaItemOperator VideoMediaItemOperator,
		ConfigModel config,
		ITagsManager tagsManager,
		IServiceProvider serviceProvider)
		: base(config, tagsManager, MediaType.Video) {
		this._VideoMediaItemOperator = VideoMediaItemOperator;
		this._serviceProvider = serviceProvider;

		FlyleafLib.Engine.Start(new FlyleafLib.EngineConfig() {
#if DEBUG
			LogOutput = ":debug",
			LogLevel = FlyleafLib.LogLevel.Debug,
			FFmpegLogLevel = Flyleaf.FFmpeg.LogLevel.Warn,
#endif
			UIRefresh = false,
			FFmpegPath = config.PathConfig.FFMpegFolderPath.Value,
		});
	}

	public override VideoMediaItemOperator CreateMediaItemOperator() {
		return this._VideoMediaItemOperator;
	}

	public override VideoMediaItemModel CreateMediaItemModelFromRecord(MediaItem MediaItem) {
		var ifm = new VideoMediaItemModel(MediaItem.MediaItemId, MediaItem.FilePath, this._VideoMediaItemOperator, this._config);
		this.SetModelProperties(ifm, MediaItem);
		return ifm;
	}

	public override VideoMediaItemViewModel CreateMediaItemViewModel(VideoMediaItemModel fileModel) {
		return new VideoMediaItemViewModel(fileModel);
	}

	public override VideoDetailViewerPreviewControlView CreateDetailViewerPreviewControlView(VideoMediaItemViewModel fileViewModel) {
		return this._videoDetailViewerPreviewControlView ??= new VideoDetailViewerPreviewControlView();
	}

	public override VideoThumbnailPickerViewModel CreateThumbnailPickerViewModel() {
		return this._serviceProvider.GetRequiredService<VideoThumbnailPickerViewModel>();
	}

	public override VideoThumbnailPickerView CreateThumbnailPickerView() {
		return new VideoThumbnailPickerView();
	}

	public override IQueryable<MediaItem> IncludeTables(IQueryable<MediaItem> MediaItems) {
		return MediaItems
			.Include(mf => mf.VideoFile);
	}
}