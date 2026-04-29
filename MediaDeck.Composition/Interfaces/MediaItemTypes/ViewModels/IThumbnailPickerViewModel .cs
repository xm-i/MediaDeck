namespace MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;

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

	public ReactiveCommand PickThumbnailFromFileCommand {
		get;
	}

	public ReactiveCommand SaveCommand {
		get;
	}

	public void RecreateThumbnail();

	public Task SaveAsync();

	public Task LoadAsync(IMediaItemViewModel fileViewModel);
}