using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Views;
using MediaDeck.Composition.Interfaces.Primitives;
using MediaDeck.Composition.Objects;

namespace MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;

public interface IMediaItemViewModel {
	public IMediaItemModel FileModel {
		get;
	}

	public string FilePath {
		get;
	}

	public BindableReactiveProperty<string> ThumbnailFilePath {
		get;
	}

	public IThumbnailControlView ThumbnailControlView {
		get;
	}

	public bool Exists {
		get;
	}

	/// <summary>
	/// プロパティ
	/// </summary>
	public Attributes<string> Properties {
		get;
	}

	public MediaType MediaType {
		get;
	}

	public IGpsLocation? Location {
		get;
	}

	public Task ExecuteFileAsync();

	public void RefreshThumbnail();
}