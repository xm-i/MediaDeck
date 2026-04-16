using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.FileTypes;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Database.Tables;
using MediaDeck.FileTypes.Base;
using MediaDeck.FileTypes.Video.Models;
using MediaDeck.FileTypes.Video.ViewModels;
using MediaDeck.FileTypes.Video.Views;
using Microsoft.Extensions.DependencyInjection;

namespace MediaDeck.FileTypes.Video;

[Inject(InjectServiceLifetime.Transient, typeof(IFileType))]
internal class VideoFileType : BaseFileType<VideoFileOperator, VideoFileModel, VideoFileViewModel, VideoDetailViewerPreviewControlView, VideoThumbnailPickerViewModel, VideoThumbnailPickerView> {
	private VideoDetailViewerPreviewControlView? _videoDetailViewerPreviewControlView;
	private readonly VideoFileOperator _videoFileOperator;
	private readonly IServiceProvider _serviceProvider;

	public VideoFileType(
		VideoFileOperator videoFileOperator,
		ConfigModel config,
		ITagsManager tagsManager,
		IServiceProvider serviceProvider)
		: base(config, tagsManager, MediaType.Video) {
		this._videoFileOperator = videoFileOperator;
		this._serviceProvider = serviceProvider;
	}

	public override VideoFileOperator CreateFileOperator() {
		return this._videoFileOperator;
	}

	public override VideoFileModel CreateFileModelFromRecord(MediaFile mediaFile) {
		var ifm = new VideoFileModel(mediaFile.MediaFileId, mediaFile.FilePath, this._videoFileOperator);
		this.SetModelProperties(ifm, mediaFile);
		return ifm;
	}

	public override VideoFileViewModel CreateFileViewModel(VideoFileModel fileModel) {
		return new VideoFileViewModel(fileModel);
	}

	public override VideoDetailViewerPreviewControlView CreateDetailViewerPreviewControlView(VideoFileViewModel fileViewModel) {
		return this._videoDetailViewerPreviewControlView ??= new VideoDetailViewerPreviewControlView();
	}

	public override VideoThumbnailPickerViewModel CreateThumbnailPickerViewModel() {
		return this._serviceProvider.GetRequiredService<VideoThumbnailPickerViewModel>();
	}

	public override VideoThumbnailPickerView CreateThumbnailPickerView() {
		return new VideoThumbnailPickerView();
	}

	public override IQueryable<MediaFile> IncludeTables(IQueryable<MediaFile> mediaFiles) {
		return mediaFiles
			.Include(mf => mf.VideoFile);
	}
}