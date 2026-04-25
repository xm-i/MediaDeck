using System.IO;



using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Views;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Composition.Objects;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Database.Tables;

namespace MediaDeck.MediaItemTypes.Base;

internal abstract class BaseMediaItemType<TFileOperator, TFileModel, TFileViewModel, TDetailViewerPreviewControlView, TThumbnailPickerViewModel, TThumbnailPickerView> : IMediaItemType<TFileOperator, TFileModel, TFileViewModel, TDetailViewerPreviewControlView, TThumbnailPickerViewModel, TThumbnailPickerView>
	where TFileOperator : IMediaItemOperator
	where TFileModel : IMediaItemModel
	where TFileViewModel : IMediaItemViewModel
	where TDetailViewerPreviewControlView : IDetailViewerPreviewControlView
	where TThumbnailPickerViewModel : IThumbnailPickerViewModel
	where TThumbnailPickerView : IThumbnailPickerView {
	protected readonly ConfigModel _config;
	protected readonly ITagsManager _tagsManager;

	internal BaseMediaItemType(ConfigModel config, ITagsManager tagsManager, MediaType mediaType) {
		this._config = config;
		this._tagsManager = tagsManager;
		this.MediaType = mediaType;
	}

	public MediaType MediaType {
		get;
	}

	public abstract TFileOperator CreateMediaItemOperator();
	public abstract TFileModel CreateMediaItemModelFromRecord(MediaItem MediaItem);
	public abstract TFileViewModel CreateMediaItemViewModel(TFileModel fileModel);
	public abstract TDetailViewerPreviewControlView CreateDetailViewerPreviewControlView(TFileViewModel fileViewModel);
	public abstract TThumbnailPickerViewModel CreateThumbnailPickerViewModel();
	public abstract TThumbnailPickerView CreateThumbnailPickerView();
	public abstract IQueryable<MediaItem> IncludeTables(IQueryable<MediaItem> MediaItems);

	protected void SetModelProperties(TFileModel fileModel, MediaItem MediaItem) {
		if (MediaItem.ThumbnailFileName != null) {
			fileModel.ThumbnailFilePath = Path.Combine(this._config.PathConfig.ThumbnailFolderPath.Value, MediaItem.ThumbnailFileName);
		}
		fileModel.Rate = MediaItem.Rate;
		fileModel.Description = MediaItem.Description;
		fileModel.UsageCount = MediaItem.UsageCount;
		fileModel.Exists = MediaItem.IsExists;
		fileModel.FileSize = MediaItem.FileSize;
		fileModel.Resolution = new ComparableSize(MediaItem.Width, MediaItem.Height);
		fileModel.CreationTime = MediaItem.CreationTime;
		fileModel.ModifiedTime = MediaItem.ModifiedTime;
		fileModel.LastAccessTime = MediaItem.LastAccessTime;
		fileModel.RegisteredTime = MediaItem.RegisteredTime;
		if (MediaItem.Latitude is { } lat && MediaItem.Longitude is { } lon) {
			fileModel.Location = new GpsLocation(lat, lon, MediaItem.Altitude);
		}
		fileModel.Tags = [.. MediaItem.MediaItemTags.Select(mft => this._tagsManager.Tags.FirstOrDefault(t => t.TagId == mft.TagId)).OfType<ITagModel>()];
	}

	IMediaItemOperator IMediaItemType.CreateMediaItemOperator() {
		return this.CreateMediaItemOperator();
	}

	IMediaItemModel IMediaItemType.CreateMediaItemModelFromRecord(MediaItem MediaItem) {
		return this.CreateMediaItemModelFromRecord(MediaItem);
	}

	IMediaItemViewModel IMediaItemType.CreateMediaItemViewModel(IMediaItemModel fileModel) {
		return this.CreateMediaItemViewModel((TFileModel)fileModel);
	}

	IDetailViewerPreviewControlView IMediaItemType.CreateDetailViewerPreviewControlView(IMediaItemViewModel fileViewModel) {
		return this.CreateDetailViewerPreviewControlView((TFileViewModel)fileViewModel);
	}

	IThumbnailPickerViewModel IMediaItemType.CreateThumbnailPickerViewModel() {
		return this.CreateThumbnailPickerViewModel();
	}

	IThumbnailPickerView IMediaItemType.CreateThumbnailPickerView() {
		return this.CreateThumbnailPickerView();
	}
}