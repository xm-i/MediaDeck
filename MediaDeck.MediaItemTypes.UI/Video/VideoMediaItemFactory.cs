using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Views;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.MediaItemTypes.Base.Models;
using MediaDeck.MediaItemTypes.Base.ViewModels;
using MediaDeck.MediaItemTypes.UI.Base.Views;
using MediaDeck.MediaItemTypes.UI.Video.Views;
using MediaDeck.MediaItemTypes.Video;
using MediaDeck.MediaItemTypes.Video.Models;
using MediaDeck.MediaItemTypes.Video.ViewModels;

namespace MediaDeck.MediaItemTypes.UI.Video;

[Inject(InjectServiceLifetime.Scoped, typeof(IMediaItemFactory))]
[Inject(InjectServiceLifetime.Scoped, typeof(IMediaItemFactoryOf<VideoMediaItemViewModel>))]
public class VideoMediaItemFactory : VideoMediaItemFactoryCore,
	IMediaItemFactory<VideoMediaItemOperator, VideoMediaItemModel, DefaultExecutionProgramObjectModel, VideoMediaItemViewModel, DefaultExecutionProgramConfigViewModel, VideoDetailViewerPreviewControlView, VideoThumbnailPickerViewModel, VideoThumbnailPickerView, DefaultExecutionConfigView>,
	IMediaItemFactoryOf<VideoMediaItemViewModel> {
	private VideoDetailViewerPreviewControlView? _videoDetailViewerPreviewControlView;

	public VideoMediaItemFactory(
		VideoMediaItemOperator VideoMediaItemOperator,
		ConfigModel config,
		ITagsManager tagsManager,
		IServiceProvider serviceProvider)
		: base(VideoMediaItemOperator, config, tagsManager, serviceProvider) {

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


	public VideoDetailViewerPreviewControlView CreateDetailViewerPreviewControlView(VideoMediaItemViewModel fileViewModel) {
		return this._videoDetailViewerPreviewControlView ??= new VideoDetailViewerPreviewControlView();
	}

	public IThumbnailControlView CreateThumbnailControlView(VideoMediaItemViewModel fileViewModel) {
		return new VideoThumbnailControlView { DataContext = fileViewModel };
	}

	public VideoThumbnailPickerView CreateThumbnailPickerView() {
		return new VideoThumbnailPickerView();
	}

	public DefaultExecutionConfigView CreateExecutionConfigView(DefaultExecutionProgramConfigViewModel viewModel) {
		return new DefaultExecutionConfigView() {
			ViewModel = viewModel
		};
	}

	public IDetailViewerPreviewControlView CreateDetailViewerPreviewControlView(IMediaItemViewModel fileViewModel) {
		return this.CreateDetailViewerPreviewControlView((VideoMediaItemViewModel)fileViewModel);
	}

	public IThumbnailControlView CreateThumbnailControlView(IMediaItemViewModel fileViewModel) {
		return this.CreateThumbnailControlView((VideoMediaItemViewModel)fileViewModel);
	}

	IThumbnailPickerView IMediaItemFactory.CreateThumbnailPickerView() {
		return this.CreateThumbnailPickerView();
	}

	public IExecutionConfigView CreateExecutionConfigView(IExecutionProgramConfigViewModel viewModel) {
		return this.CreateExecutionConfigView((DefaultExecutionProgramConfigViewModel)viewModel);
	}
}