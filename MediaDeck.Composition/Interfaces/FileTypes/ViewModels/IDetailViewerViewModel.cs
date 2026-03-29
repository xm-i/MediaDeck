namespace MediaDeck.Composition.Interfaces.FileTypes.ViewModels;

public interface IDetailViewerViewModel {
	public BindableReactiveProperty<string?> SelectedFilePath {
		get;
	}

	public BindableReactiveProperty<string?> SelectedFileThumbnailFilePath {
		get;
	}
}