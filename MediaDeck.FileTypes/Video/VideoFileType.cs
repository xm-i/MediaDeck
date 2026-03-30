using CommunityToolkit.Mvvm.DependencyInjection;

using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.FileTypes;
using MediaDeck.Database.Tables;
using MediaDeck.FileTypes.Base;
using MediaDeck.FileTypes.Video.Models;
using MediaDeck.FileTypes.Video.ViewModels;
using MediaDeck.FileTypes.Video.Views;

namespace MediaDeck.FileTypes.Video;

[Inject(InjectServiceLifetime.Transient, typeof(IFileType))]
internal class VideoFileType : BaseFileType<VideoFileOperator, VideoFileModel, VideoFileViewModel, VideoDetailViewerPreviewControlView, VideoThumbnailPickerViewModel, VideoThumbnailPickerView> {
	private VideoDetailViewerPreviewControlView? _videoDetailViewerPreviewControlView;
	private readonly VideoFileOperator _videoFileOperator;

	public VideoFileType(VideoFileOperator videoFileOperator) : base(MediaType.Video) {
		this._videoFileOperator = videoFileOperator;
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
		return Ioc.Default.GetRequiredService<VideoThumbnailPickerViewModel>();
	}

	public override VideoThumbnailPickerView CreateThumbnailPickerView() {
		return new VideoThumbnailPickerView();
	}

	public override IQueryable<MediaFile> IncludeTables(IQueryable<MediaFile> mediaFiles) {
		return mediaFiles
			.Include(mf => mf.VideoFile);
	}
}