using System.Threading.Tasks;

using MediaDeck.FileTypes.Base.ViewModels;
using MediaDeck.FileTypes.Base.ViewModels.Interfaces;
using MediaDeck.FileTypes.Video.Models;
using MediaDeck.Models.FileDetailManagers;

namespace MediaDeck.FileTypes.Video.ViewModels;

[AddTransient]
public class VideoThumbnailPickerViewModel : BaseThumbnailPickerViewModel {
	public VideoThumbnailPickerViewModel(
		ThumbnailsManager thumbnailsManager,
		VideoFileOperator videoFileOperator) : base(thumbnailsManager) {
		this._videoFileOperator = videoFileOperator;
		this._updateTimeSubject.ObserveOnCurrentSynchronizationContext().Subscribe(x => this.Time.Value = x);
	}
	private readonly VideoFileOperator _videoFileOperator;

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
			this.CandidateThumbnail.Value = this._videoFileOperator.CreateThumbnail(this.targetFileViewModel.FileModel, 300, 300, this.Time.Value);
		} catch (Exception) {
			this.CandidateThumbnail.Value = null;
		}
	}

	public override async Task LoadAsync(IFileViewModel fileViewModel) {
		await base.LoadAsync(fileViewModel);
		this.VideoFilePath.Value = fileViewModel.FilePath;
	}

	public void UpdateTime(TimeSpan time) {
		this._updateTimeSubject.OnNext(time);
	}
}
