using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Views;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Database.Tables;
using MediaDeck.MediaItemTypes.Base.Models;
using MediaDeck.MediaItemTypes.Base.ViewModels;
using MediaDeck.MediaItemTypes.UI.Base;
using MediaDeck.MediaItemTypes.UI.Base.Views;
using MediaDeck.MediaItemTypes.UI.Video.Views;
using MediaDeck.MediaItemTypes.Video.Models;
using MediaDeck.MediaItemTypes.Video.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace MediaDeck.MediaItemTypes.UI.Video;

[Inject(InjectServiceLifetime.Singleton, typeof(IMediaItemType))]
public class VideoMediaItemType : BaseMediaItemType<VideoMediaItemOperator, VideoMediaItemModel, DefaultExecutionProgramObjectModel, VideoMediaItemViewModel, DefaultExecutionProgramConfigViewModel, VideoDetailViewerPreviewControlView, VideoThumbnailPickerViewModel, VideoThumbnailPickerView, DefaultExecutionConfigView> {
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

	public override ItemType ItemType {
		get {
			return ItemType.Video;
		}
	}

	public override VideoMediaItemModel CreateMediaItemModelFromRecord(MediaItem MediaItem, IServiceProvider scopedServiceProvider) {
		var ifm = new VideoMediaItemModel(MediaItem.MediaItemId, MediaItem.FilePath, this._VideoMediaItemOperator, this, scopedServiceProvider);
		this.SetModelProperties(ifm, MediaItem);
		return ifm;
	}

	public override VideoMediaItemViewModel CreateMediaItemViewModel(VideoMediaItemModel fileModel) {
		return new VideoMediaItemViewModel(fileModel, this);
	}

	public override VideoDetailViewerPreviewControlView CreateDetailViewerPreviewControlView(VideoMediaItemViewModel fileViewModel) {
		return this._videoDetailViewerPreviewControlView ??= new VideoDetailViewerPreviewControlView();
	}

	public override VideoThumbnailPickerViewModel CreateThumbnailPickerViewModel() {
		return this._serviceProvider.GetRequiredService<VideoThumbnailPickerViewModel>();
	}

	public override IThumbnailControlView CreateThumbnailControlView(VideoMediaItemViewModel fileViewModel) {
		return new VideoThumbnailControlView { DataContext = fileViewModel };
	}

	public override VideoThumbnailPickerView CreateThumbnailPickerView() {
		return new VideoThumbnailPickerView();
	}

	public override DefaultExecutionProgramObjectModel CreateExecutionProgramObjectModel() {
		return new DefaultExecutionProgramObjectModel() {
			MediaType = this.MediaType
		};
	}

	public override DefaultExecutionProgramConfigViewModel CreateExecutionProgramConfigViewModel(DefaultExecutionProgramObjectModel model) {
		return new DefaultExecutionProgramConfigViewModel(model, this._serviceProvider.GetRequiredService<IMediaItemTypeService>(), this._serviceProvider.GetRequiredService<ExecutionConfigModel>());
	}

	public override DefaultExecutionConfigView CreateExecutionConfigView(DefaultExecutionProgramConfigViewModel viewModel) {
		return new DefaultExecutionConfigView() {
			ViewModel = viewModel
		};
	}

	public override IQueryable<MediaItem> IncludeTables(IQueryable<MediaItem> MediaItems) {
		return MediaItems
			.Include(mf => mf.VideoFile);
	}
}