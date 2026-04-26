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
public class MediaItemTypeService(IEnumerable<IMediaItemType> MediaItemTypes) : IMediaItemTypeService {
	private readonly IMediaItemType[] _MediaItemTypes = MediaItemTypes.ToArray();
	private readonly IMediaItemType _UnknownMediaItemType = MediaItemTypes.First(x => x.MediaType == MediaType.Unknown);

	/// <inheritdoc />
	public IMediaItemModel CreateMediaItemModelFromRecord(MediaItem MediaItem, IServiceProvider scopedServiceProvider) {
		return this.GetMediaItemType(MediaItem).CreateMediaItemModelFromRecord(MediaItem, scopedServiceProvider);
	}

	/// <inheritdoc />
	public IMediaItemViewModel CreateMediaItemViewModel(IMediaItemModel fileModel) {
		return this.GetMediaItemType(fileModel).CreateMediaItemViewModel(fileModel);
	}

	/// <inheritdoc />
	public IDetailViewerPreviewControlView CreateDetailViewerPreviewControlView(IMediaItemViewModel fileViewModel) {
		return this.GetMediaItemType(fileViewModel).CreateDetailViewerPreviewControlView(fileViewModel);
	}

	/// <inheritdoc />
	public IThumbnailPickerViewModel CreateThumbnailPickerViewModel(IMediaItemViewModel fileViewModel) {
		return this.GetMediaItemType(fileViewModel).CreateThumbnailPickerViewModel();
	}

	/// <inheritdoc />
	public IThumbnailPickerView CreateThumbnailPickerView(IMediaItemViewModel fileViewModel) {
		return this.GetMediaItemType(fileViewModel).CreateThumbnailPickerView();
	}

	/// <inheritdoc />
	public IMediaItemOperator[] CreateMediaItemOperators() {
		return this._MediaItemTypes.Select(x => x.CreateMediaItemOperator()).ToArray();
	}

	/// <inheritdoc />
	public IQueryable<MediaItem> IncludeTables(IQueryable<MediaItem> MediaItems) {
		var result = MediaItems;
		foreach (var MediaItemType in this._MediaItemTypes) {
			result = MediaItemType.IncludeTables(result);
		}
		return result;
	}

	/// <inheritdoc />
	public IMediaItemType GetMediaItemType(string path) {
		return this._MediaItemTypes.FirstOrDefault(x => x.IsTargetPath(path)) ?? this._UnknownMediaItemType;
	}

	/// <inheritdoc />
	public IMediaItemType GetMediaItemType(MediaType mediaType) {
		return this._MediaItemTypes.First(x => x.MediaType == mediaType);
	}

	/// <inheritdoc />
	public IMediaItemType GetMediaItemType(MediaItem MediaItem) {
		return this._MediaItemTypes.FirstOrDefault(x => x.ItemType == MediaItem.ItemType) ?? this._UnknownMediaItemType;
	}

	/// <inheritdoc />
	public bool IsTargetPath(string path) {
		return this._MediaItemTypes.Any(x => x.MediaType != MediaType.Unknown && x.IsTargetPath(path));
	}

	/// <inheritdoc />
	public bool IsTargetPath(string path, MediaType mediaType) {
		var mediaItemType = this._MediaItemTypes.FirstOrDefault(x => x.MediaType == mediaType);
		return mediaItemType is not null && mediaItemType.IsTargetPath(path);
	}

	/// <inheritdoc />
	public IExecutionProgramObjectModel CreateExecutionProgramObjectModel(MediaType mediaType) {
		var mediaItemType = this._MediaItemTypes.First(x => x.MediaType == mediaType);
		return mediaItemType.CreateExecutionProgramObjectModel();
	}

	/// <inheritdoc />
	public IExecutionProgramConfigViewModel CreateExecutionConfigViewModel(IExecutionProgramObjectModel model) {
		var mediaItemType = this._MediaItemTypes.First(x => x.MediaType == model.MediaType);
		return mediaItemType.CreateExecutionProgramConfigViewModel(model);
	}

	/// <inheritdoc />
	public IExecutionConfigView CreateExecutionConfigView(IExecutionProgramConfigViewModel viewModel) {
		var mediaItemType = this._MediaItemTypes.First(x => x.MediaType == viewModel.MediaType);
		return mediaItemType.CreateExecutionConfigView(viewModel);
	}

	private IMediaItemType GetMediaItemType(IMediaItemModel fileModel) {
		return this._MediaItemTypes.FirstOrDefault(x => x.MediaType == fileModel.MediaType) ?? this._UnknownMediaItemType;
	}

	private IMediaItemType GetMediaItemType(IMediaItemViewModel fileViewModel) {
		return this._MediaItemTypes.FirstOrDefault(x => x.MediaType == fileViewModel.MediaType) ?? this._UnknownMediaItemType;
	}
}