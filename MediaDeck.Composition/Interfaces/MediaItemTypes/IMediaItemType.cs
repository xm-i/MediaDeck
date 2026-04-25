using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Views;
using MediaDeck.Database.Tables;

namespace MediaDeck.Composition.Interfaces.MediaItemTypes;

public interface IMediaItemType<TFileOperator, TFileModel, TFileViewModel, TDetailViewerPreviewControlView, TThumbnailPickerViewModel, TThumbnailPickerView> : IMediaItemType
	where TFileOperator : IMediaItemOperator
	where TFileModel : IMediaItemModel
	where TFileViewModel : IMediaItemViewModel
	where TDetailViewerPreviewControlView : IDetailViewerPreviewControlView
	where TThumbnailPickerViewModel : IThumbnailPickerViewModel
	where TThumbnailPickerView : IThumbnailPickerView {
	public new TFileOperator CreateMediaItemOperator();
	public new TFileModel CreateMediaItemModelFromRecord(MediaItem MediaItem);
	public TDetailViewerPreviewControlView CreateDetailViewerPreviewControlView(TFileViewModel fileViewModel);
	public TFileViewModel CreateMediaItemViewModel(TFileModel fileModel);
	public new TThumbnailPickerViewModel CreateThumbnailPickerViewModel();
	public new TThumbnailPickerView CreateThumbnailPickerView();
}

public interface IMediaItemType {
	public MediaType MediaType {
		get;
	}

	public ItemType ItemType {
		get;
	}

	public IMediaItemOperator CreateMediaItemOperator();
	public IMediaItemModel CreateMediaItemModelFromRecord(MediaItem MediaItem);
	public IDetailViewerPreviewControlView CreateDetailViewerPreviewControlView(IMediaItemViewModel fileViewModel);
	public IMediaItemViewModel CreateMediaItemViewModel(IMediaItemModel fileModel);
	public IThumbnailPickerViewModel CreateThumbnailPickerViewModel();
	public IThumbnailPickerView CreateThumbnailPickerView();
	public IQueryable<MediaItem> IncludeTables(IQueryable<MediaItem> MediaItems);
	public bool IsTargetPath(string path);
	public MediaItemPathStatus GetPathStatus(string path);
}

public readonly struct MediaItemPathStatus {
	public MediaItemPathStatus(bool exists, long fileSize, DateTime creationTime, DateTime modifiedTime, DateTime lastAccessTime) {
		this.Exists = exists;
		this.FileSize = fileSize;
		this.CreationTime = creationTime;
		this.ModifiedTime = modifiedTime;
		this.LastAccessTime = lastAccessTime;
	}

	public bool Exists {
		get;
	}

	public long FileSize {
		get;
	}

	public DateTime CreationTime {
		get;
	}

	public DateTime ModifiedTime {
		get;
	}

	public DateTime LastAccessTime {
		get;
	}
}