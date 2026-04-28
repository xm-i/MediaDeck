using System.Threading.Tasks;

using MediaDeck.Common.Base;
using MediaDeck.Composition.Constants;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Views;
using MediaDeck.Composition.Interfaces.Primitives;
using MediaDeck.Composition.Objects;

namespace MediaDeck.MediaItemTypes.Base.ViewModels;

public abstract class BaseMediaItemViewModel : ViewModelBase, IMediaItemViewModel {
	protected BaseMediaItemViewModel(IMediaItemModel fileModel, IMediaItemFactory mediaItemFactory, MediaType mediaType) {
		this.FileModel = fileModel;
		this.FilePath = fileModel.FilePath;
		this.ThumbnailFilePath = new($"file:///{fileModel.ThumbnailFilePath ?? FilePathConstants.NoThumbnailFilePath}");
		this.Exists = fileModel.Exists;
		this.Properties = fileModel.Properties;
		this.MediaType = mediaType;
		this.Location = fileModel.Location;
		this._mediaItemFactory = mediaItemFactory;
	}

	private long _thumbnailRefreshTicks = 0;
	private readonly IMediaItemFactory _mediaItemFactory;

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
		get {
			return this._mediaItemFactory.CreateThumbnailControlView(this);
		}
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

	public virtual async Task ExecuteFileAsync() {
		await this.FileModel.ExecuteFileAsync();
	}

	public void RefreshThumbnail() {
		this._thumbnailRefreshTicks = DateTime.Now.Ticks;
		this.ThumbnailFilePath.Value = $"file:///{this.FileModel.ThumbnailFilePath ?? FilePathConstants.NoThumbnailFilePath}?refresh={this._thumbnailRefreshTicks}";
	}
}