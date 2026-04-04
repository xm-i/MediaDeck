using System.Threading.Tasks;
using MediaDeck.Composition.Interfaces.FileTypes.ViewModels;
using MediaDeck.FileTypes.Base.Models;
using MediaDeck.FileTypes.Base.ViewModels;
using MediaDeck.FileTypes.Video.Models;
using Microsoft.Extensions.Logging;

namespace MediaDeck.FileTypes.Video.ViewModels;

[Inject(InjectServiceLifetime.Transient)]
internal class VideoThumbnailPickerViewModel : BaseThumbnailPickerViewModel {
	public VideoThumbnailPickerViewModel(
		BaseThumbnailPickerModel thumbnailPickerModel,
		VideoFileOperator videoFileOperator,
		ILogger<VideoThumbnailPickerViewModel> logger) : base(thumbnailPickerModel) {
		this._videoFileOperator = videoFileOperator;
		this._logger = logger;
		this._updateTimeSubject.ObserveOnCurrentSynchronizationContext().Subscribe(x => this.Time.Value = x);
	}

	private readonly VideoFileOperator _videoFileOperator;

	private readonly ILogger<VideoThumbnailPickerViewModel> _logger;

	private readonly Subject<TimeSpan> _updateTimeSubject = new();

	internal BindableReactiveProperty<TimeSpan> Time {
		get;
	} = new();

	internal BindableReactiveProperty<string> VideoFilePath {
		get;
	} = new();

	public override void RecreateThumbnail() {
		if (this.targetFileViewModel is null) {
			return;
		}
		try {
			this.CandidateThumbnail.Value = this._videoFileOperator.CreateThumbnail(this.targetFileViewModel.FileModel, 300, 300, this.Time.Value);
		} catch (Exception ex) {
			this._logger.LogError(ex, "Failed to recreate video thumbnail for file {FilePath}", this.targetFileViewModel.FilePath);
			this.CandidateThumbnail.Value = null;
		}
	}

	public override async Task LoadAsync(IFileViewModel fileViewModel) {
		await base.LoadAsync(fileViewModel);
		this.VideoFilePath.Value = fileViewModel.FilePath;
	}

	internal void UpdateTime(TimeSpan time) {
		this._updateTimeSubject.OnNext(time);
	}
}