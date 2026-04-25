namespace MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;

public interface IDetailViewerViewModel {
	/// <summary>
	/// このViewerが選択されているかどうか。
	/// </summary>
	public BindableReactiveProperty<bool> IsSelected {
		get;
	}

	/// <summary>
	/// 選択中のファイルのViewModel。
	/// </summary>
	public BindableReactiveProperty<IMediaItemViewModel?> SelectedFile {
		get;
	}

	/// <summary>
	/// 選択中のファイルパス
	/// </summary>
	public BindableReactiveProperty<string?> SelectedFilePath {
		get;
	}

	/// <summary>
	/// 選択中のファイルのサムネイルファイルパス
	/// </summary>
	public BindableReactiveProperty<string?> SelectedFileThumbnailFilePath {
		get;
	}
}