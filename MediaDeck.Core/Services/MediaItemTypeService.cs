using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Views;
using MediaDeck.Database.Tables;

using DbItemType = MediaDeck.Database.Tables.ItemType;

namespace MediaDeck.Core.Services;

/// <summary>
/// メディアアイテムタイプに関連する操作を提供するサービス実装クラス
/// </summary>
[Inject(InjectServiceLifetime.Singleton, typeof(IMediaItemTypeService))]
public class MediaItemTypeService(IEnumerable<IMediaItemType> MediaItemTypes) : IMediaItemTypeService {
	private readonly IMediaItemType[] _MediaItemTypes = MediaItemTypes.ToArray();
	private readonly IMediaItemType _UnknownMediaItemType = MediaItemTypes.First(x => x.MediaType == MediaType.Unknown);

	/// <inheritdoc />
	public IMediaItemModel CreateMediaItemModelFromRecord(MediaItem MediaItem) {
		return this.GetMediaItemType(MediaItem).CreateMediaItemModelFromRecord(MediaItem);
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

	private IMediaItemType GetMediaItemType(MediaItem MediaItem) {
		var mediaType = MediaItem.ItemType switch {
			DbItemType.Image => MediaType.Image,
			DbItemType.Video => MediaType.Video,
			DbItemType.Pdf => MediaType.Pdf,
			DbItemType.Archive => MediaType.Archive,
			DbItemType.FolderGroup => MediaType.FolderGroup,
			_ => MediaType.Unknown
		};
		return this._MediaItemTypes.FirstOrDefault(x => x.MediaType == mediaType) ?? this._UnknownMediaItemType;
	}

	private IMediaItemType GetMediaItemType(IMediaItemModel fileModel) {
		return this._MediaItemTypes.FirstOrDefault(x => x.MediaType == fileModel.MediaType) ?? this._UnknownMediaItemType;
	}

	private IMediaItemType GetMediaItemType(IMediaItemViewModel fileViewModel) {
		return this._MediaItemTypes.FirstOrDefault(x => x.MediaType == fileViewModel.MediaType) ?? this._UnknownMediaItemType;
	}
}