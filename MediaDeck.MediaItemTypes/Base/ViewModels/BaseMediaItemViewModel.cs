using System.Threading.Tasks;

using MediaDeck.Common.Base;
using MediaDeck.Composition.Constants;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Composition.Interfaces.Primitives;
using MediaDeck.Composition.Objects;

namespace MediaDeck.MediaItemTypes.Base.ViewModels;

public abstract class BaseMediaItemViewModel : ViewModelBase, IMediaItemViewModel {
	protected BaseMediaItemViewModel(MediaType mediaType) : base() {
		this.MediaType = mediaType;
	}

	public virtual void Initialize(IMediaItemModel fileModel) {
		this._fileModel = fileModel;
		this._filePath = fileModel.FilePath;
		this._thumbnailFilePath = new($"file:///{fileModel.ThumbnailFilePath ?? FilePathConstants.NoThumbnailFilePath}");
		this._exists = fileModel.Exists;
		this._properties = fileModel.Properties;
		this.Location = fileModel.Location;
		this._thumbnailFilePath.AddTo(this.CompositeDisposable);
	}

	private InvalidOperationException CreateNotInitializedException() {
		return new($"{this.GetType().Name} is not initialized.");
	}

	private long _thumbnailRefreshTicks = 0;

	private IMediaItemModel? _fileModel;
	public IMediaItemModel FileModel {
		get {
			return this._fileModel ?? throw this.CreateNotInitializedException();
		}
	}

	private string? _filePath;
	public string FilePath {
		get {
			return this._filePath ?? throw this.CreateNotInitializedException();
		}
	}

	private BindableReactiveProperty<string>? _thumbnailFilePath;
	public BindableReactiveProperty<string> ThumbnailFilePath {
		get {
			return this._thumbnailFilePath ?? throw this.CreateNotInitializedException();
		}
	}

	private bool? _exists;
	public bool Exists {
		get {
			return this._exists ?? throw this.CreateNotInitializedException();
		}
	}

	private Attributes<string>? _properties;
	/// <summary>
	/// プロパティ
	/// </summary>
	public Attributes<string> Properties {
		get {
			return this._properties ?? throw this.CreateNotInitializedException();
		}
	}

	public MediaType MediaType {
		get;
		private set;
	}

	public IGpsLocation? Location {
		get;
		private set;
	}

	public virtual async Task ExecuteFileAsync() {
		await this.FileModel.ExecuteFileAsync();
	}

	public void RefreshThumbnail() {
		this._thumbnailRefreshTicks = DateTime.Now.Ticks;
		this.ThumbnailFilePath.Value = $"file:///{this.FileModel.ThumbnailFilePath ?? FilePathConstants.NoThumbnailFilePath}?refresh={this._thumbnailRefreshTicks}";
	}
}