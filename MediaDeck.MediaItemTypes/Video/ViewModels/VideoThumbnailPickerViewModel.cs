using System.Threading.Tasks;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Composition.Stores.Config.Model;
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
		IFilePickerService filePickerService,
		ConfigModel config) : base(thumbnailPickerModel, filePickerService, config) {
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

	public override async Task RecreateThumbnailAsync() {
		if (this.targetFileViewModel is null) {
			return;
		}
		try {
			this.CandidateThumbnail.Value = await this._VideoMediaItemOperator.CreateThumbnailAsync(this.targetFileViewModel.FileModel, this._config.ThumbnailConfig.ThumbnailSize.Value, this._config.ThumbnailConfig.ThumbnailSize.Value, this.Time.Value);
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