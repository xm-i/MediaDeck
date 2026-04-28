using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Views;
using MediaDeck.Database.Tables;

namespace MediaDeck.Core.Services;

/// <summary>
/// メディアアイテムタイプに関連する操作を提供するサービス実装クラス
/// </summary>
[Inject(InjectServiceLifetime.Singleton, typeof(IMediaItemTypeService))]
public class MediaItemTypeService(IEnumerable<IMediaItemFactory> mediaItemFactories, IEnumerable<IMediaItemTypeProvider> mediaItemTypeProviders) : IMediaItemTypeService {
	private readonly IMediaItemFactory[] _mediaItemFactories = mediaItemFactories.ToArray();
	private readonly IMediaItemFactory _unknownMediaItemFactory = mediaItemFactories.First(x => x.MediaType == MediaType.Unknown);
	private readonly IMediaItemTypeProvider[] _mediaItemTypeProviders = mediaItemTypeProviders.ToArray();
	private readonly IMediaItemTypeProvider _unknownMediaItemProvider = mediaItemTypeProviders.First(x => x.MediaType == MediaType.Unknown);

	/// <inheritdoc />
	public IMediaItemModel CreateMediaItemModelFromRecord(MediaItem MediaItem, IServiceProvider scopedServiceProvider) {
		return this.GetMediaItemFactory(MediaItem).CreateMediaItemModelFromRecord(MediaItem, scopedServiceProvider);
	}

	/// <inheritdoc />
	public IMediaItemViewModel CreateMediaItemViewModel(IMediaItemModel fileModel) {
		return this.GetMediaItemFactory(fileModel).CreateMediaItemViewModel(fileModel);
	}

	/// <inheritdoc />
	public IDetailViewerPreviewControlView CreateDetailViewerPreviewControlView(IMediaItemViewModel fileViewModel) {
		return this.GetMediaItemFactory(fileViewModel).CreateDetailViewerPreviewControlView(fileViewModel);
	}

	/// <inheritdoc />
	public IThumbnailPickerViewModel CreateThumbnailPickerViewModel(IMediaItemViewModel fileViewModel) {
		return this.GetMediaItemFactory(fileViewModel).CreateThumbnailPickerViewModel();
	}

	/// <inheritdoc />
	public IThumbnailPickerView CreateThumbnailPickerView(IMediaItemViewModel fileViewModel) {
		return this.GetMediaItemFactory(fileViewModel).CreateThumbnailPickerView();
	}

	/// <inheritdoc />
	public IMediaItemOperator[] CreateMediaItemOperators() {
		return this._mediaItemFactories.Select(x => x.CreateMediaItemOperator()).ToArray();
	}

	/// <inheritdoc />
	public IQueryable<MediaItem> IncludeTables(IQueryable<MediaItem> MediaItems) {
		var result = MediaItems;
		foreach (var provider in this._mediaItemTypeProviders) {
			result = provider.IncludeTables(result);
		}
		return result;
	}

	/// <inheritdoc />
	public IMediaItemFactory GetMediaItemFactory(string path) {
		var type = this._mediaItemTypeProviders.FirstOrDefault(x => x.IsTargetPath(path))?.MediaType ?? this._unknownMediaItemFactory.MediaType;
		return this.GetMediaItemFactory(type);
	}

	/// <inheritdoc />
	public IMediaItemFactory GetMediaItemFactory(MediaType mediaType) {
		return this._mediaItemFactories.First(x => x.MediaType == mediaType);
	}

	/// <inheritdoc />
	public IMediaItemFactory GetMediaItemFactory(MediaItem MediaItem) {
		return this._mediaItemFactories.FirstOrDefault(x => x.ItemType == MediaItem.ItemType) ?? this._unknownMediaItemFactory;
	}

	public IMediaItemTypeProvider GetMediaItemTypeProvider(ItemType mediaType) {
		var factory = this._mediaItemFactories.FirstOrDefault(x => x.ItemType == mediaType);
		return this._mediaItemTypeProviders.FirstOrDefault(x => x.MediaType == factory?.MediaType) ?? this._unknownMediaItemProvider;
	}

	/// <inheritdoc />
	public bool IsTargetPath(string path) {
		return this._mediaItemTypeProviders.Any(x => x.IsTargetPath(path));
	}

	/// <inheritdoc />
	public bool IsTargetPath(string path, MediaType mediaType) {
		var mediaItemTypeProvider = this._mediaItemTypeProviders.FirstOrDefault(x => x.MediaType == mediaType);
		return mediaItemTypeProvider is not null && mediaItemTypeProvider.IsTargetPath(path);
	}

	/// <inheritdoc />
	public IExecutionProgramObjectModel CreateExecutionProgramObjectModel(MediaType mediaType) {
		var mediaItemFactory = this._mediaItemFactories.First(x => x.MediaType == mediaType);
		return mediaItemFactory.CreateExecutionProgramObjectModel();
	}

	/// <inheritdoc />
	public IExecutionProgramConfigViewModel CreateExecutionConfigViewModel(IExecutionProgramObjectModel model) {
		var mediaItemFactory = this._mediaItemFactories.First(x => x.MediaType == model.MediaType);
		return mediaItemFactory.CreateExecutionProgramConfigViewModel(model);
	}

	/// <inheritdoc />
	public IExecutionConfigView CreateExecutionConfigView(IExecutionProgramConfigViewModel viewModel) {
		var mediaItemFactory = this._mediaItemFactories.First(x => x.MediaType == viewModel.MediaType);
		return mediaItemFactory.CreateExecutionConfigView(viewModel);
	}

	private IMediaItemFactory GetMediaItemFactory(IMediaItemModel fileModel) {
		return this._mediaItemFactories.FirstOrDefault(x => x.MediaType == fileModel.MediaType) ?? this._unknownMediaItemFactory;
	}

	private IMediaItemFactory GetMediaItemFactory(IMediaItemViewModel fileViewModel) {
		return this._mediaItemFactories.FirstOrDefault(x => x.MediaType == fileViewModel.MediaType) ?? this._unknownMediaItemFactory;
	}
}