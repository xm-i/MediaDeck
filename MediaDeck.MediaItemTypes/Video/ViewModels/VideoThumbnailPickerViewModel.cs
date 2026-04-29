using System.Threading.Tasks;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.MediaItemTypes.Base.Models;
using MediaDeck.MediaItemTypes.Base.ViewModels;
using MediaDeck.MediaItemTypes.Video.Models;
using Microsoft.Extensions.Logging;

namespace MediaDeck.MediaItemTypes.Video.ViewModels;

[Inject(InjectServiceLifetime.Transient)]
public class VideoThumbnailPickerViewModel : BaseThumbnailPickerViewModel<BaseThumbnailPickerModel> {
	public VideoThumbnailPickerViewModel(
		BaseThumbnailPickerModel thumbnailPickerModel,
		VideoMediaItemOperator VideoMediaItemOperator,
		ILogger<VideoThumbnailPickerViewModel> logger,
		IFilePickerService filePickerService) : base(thumbnailPickerModel, filePickerService) {
		this._VideoMediaItemOperator = VideoMediaItemOperator;
		this._logger = logger;
		this._updateTimeSubject.ObserveOnCurrentSynchronizationContext().Subscribe(x => this.Time.Value = x).AddTo(this.CompositeDisposable);
	}

	private readonly VideoMediaItemOperator _VideoMediaItemOperator;

	private readonly ILogger<VideoThumbnailPickerViewModel> _logger;

	private readonly Subject<TimeSpan> _updateTimeSubject = new();

	public BindableReactiveProperty<TimeSpan> Time {
		get;
	} = new();

	public BindableReactiveProperty<string> VideoFilePath {
		get;
	} = new();

	public override void RecreateThumbnail() {
		if (this.targetFileViewModel is null) {
			return;
		}
		try {
			this.CandidateThumbnail.Value = this._VideoMediaItemOperator.CreateThumbnail(this.targetFileViewModel.FileModel, 300, 300, this.Time.Value);
		} catch (Exception ex) {
			this._logger.LogError(ex, "Failed to recreate video thumbnail for file {FilePath}", this.targetFileViewModel.FilePath);
			this.CandidateThumbnail.Value = null;
		}
	}

	public override async Task LoadAsync(IMediaItemViewModel fileViewModel) {
		await base.LoadAsync(fileViewModel);
		this.VideoFilePath.Value = fileViewModel.FilePath;
	}

	public void UpdateTime(TimeSpan time) {
		this._updateTimeSubject.OnNext(time);
	}
}