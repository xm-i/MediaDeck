using System.Threading.Tasks;

using MapControl;

using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.FileTypes.Models;
using MediaDeck.Composition.Objects;

namespace MediaDeck.FileTypes.Base.ViewModels.Interfaces;

public interface IFileViewModel {
	public IFileModel FileModel {
		get;
	}

	public string FilePath {
		get;
	}

	public BindableReactiveProperty<string> ThumbnailFilePath {
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

	public Location? Location {
		get;
	}

	public Task ExecuteFileAsync();

	public void RefreshThumbnail();
}
