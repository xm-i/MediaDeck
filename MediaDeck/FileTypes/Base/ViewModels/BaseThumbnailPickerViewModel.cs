using System.Threading.Tasks;

using MediaDeck.FileTypes.Base.ViewModels.Interfaces;
using MediaDeck.Models.FileDetailManagers;

namespace MediaDeck.FileTypes.Base.ViewModels;

public abstract class BaseThumbnailPickerViewModel : IThumbnailPickerViewModel {
	public BaseThumbnailPickerViewModel(ThumbnailsManager thumbnailsManager) {
		this.RecreateThumbnailCommand.Subscribe(_ => this.RecreateThumbnail());
		this.SaveCommand.Subscribe(async _ => await this.SaveAsync());
		this.thumbnailsManager = thumbnailsManager;
	}

	protected IFileViewModel? targetFileViewModel;
	protected ThumbnailsManager thumbnailsManager;

	public BindableReactiveProperty<byte[]?> OriginalThumbnail {
		get;
	} = new();

	public BindableReactiveProperty<byte[]?> CandidateThumbnail {
		get;
	} = new();

	public ReactiveCommand RecreateThumbnailCommand {
		get;
	} = new();

	public ReactiveCommand SaveCommand {
		get;
	} = new();

	public abstract void RecreateThumbnail();

	public virtual async Task SaveAsync() {
		if (this.targetFileViewModel is null) {
			return;
		}
		if (this.CandidateThumbnail.Value is null) {
			return;
		}
		await this.thumbnailsManager.UpdateThumbnailAsync(this.targetFileViewModel.FileModel, this.CandidateThumbnail.Value);
		this.targetFileViewModel.RefreshThumbnail();
	}

	public virtual async Task LoadAsync(IFileViewModel fileViewModel) {
		this.targetFileViewModel = fileViewModel;
		this.CandidateThumbnail.Value = null;
		this.OriginalThumbnail.Value = await this.thumbnailsManager.LoadThumbnailAsync(fileViewModel.FileModel);
	}
}
