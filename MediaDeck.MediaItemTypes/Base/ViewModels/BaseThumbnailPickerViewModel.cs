using System.Threading.Tasks;

using MediaDeck.Common.Base;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.MediaItemTypes.Base.Models;

namespace MediaDeck.MediaItemTypes.Base.ViewModels;

public abstract class BaseThumbnailPickerViewModel<TThumbnailPickerModel> : ViewModelBase, IThumbnailPickerViewModel
	where TThumbnailPickerModel : BaseThumbnailPickerModel {
	public BaseThumbnailPickerViewModel(TThumbnailPickerModel thumbnailPickerModel) {
		this.RecreateThumbnailCommand.Subscribe(_ => this.RecreateThumbnail()).AddTo(this.CompositeDisposable);
		this.SaveCommand.Subscribe(async _ => await this.SaveAsync()).AddTo(this.CompositeDisposable);
		this.thumbnailPickerModel = thumbnailPickerModel;

	}

	protected IMediaItemViewModel? targetFileViewModel;
	protected TThumbnailPickerModel thumbnailPickerModel;

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
		await this.thumbnailPickerModel.UpdateThumbnailAsync(this.targetFileViewModel.FileModel, this.CandidateThumbnail.Value);
		this.targetFileViewModel.RefreshThumbnail();
	}

	public virtual async Task LoadAsync(IMediaItemViewModel fileViewModel) {
		this.targetFileViewModel = fileViewModel;
		this.CandidateThumbnail.Value = null;
		this.OriginalThumbnail.Value = await this.thumbnailPickerModel.LoadThumbnailAsync(fileViewModel.FileModel);
	}
}