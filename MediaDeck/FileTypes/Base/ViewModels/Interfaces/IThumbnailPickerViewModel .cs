using System.Threading.Tasks;

namespace MediaDeck.FileTypes.Base.ViewModels.Interfaces;

public interface IThumbnailPickerViewModel {
	public BindableReactiveProperty<byte[]?> OriginalThumbnail {
		get;
	}

	public BindableReactiveProperty<byte[]?> CandidateThumbnail {
		get;
	}

	public ReactiveCommand RecreateThumbnailCommand {
		get;
	}

	public ReactiveCommand SaveCommand {
		get;
	}

	public void RecreateThumbnail();

	public Task SaveAsync();

	public Task LoadAsync(IFileViewModel fileViewModel);
}
