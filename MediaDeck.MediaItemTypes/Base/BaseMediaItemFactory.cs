using System.IO;

using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Composition.Objects;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Database.Tables;

namespace MediaDeck.MediaItemTypes.Base;

public abstract class BaseMediaItemFactory<TFileOperator, TFileModel, TExecutionProgramObjectModel, TFileViewModel, TExecutionProgramConfigViewModel, TThumbnailPickerViewModel>
	: IMediaItemFactory<TFileOperator, TFileModel, TExecutionProgramObjectModel, TFileViewModel, TExecutionProgramConfigViewModel, TThumbnailPickerViewModel>
	where TFileOperator : IMediaItemOperator
	where TFileModel : IMediaItemModel
	where TExecutionProgramObjectModel : IExecutionProgramObjectModel
	where TFileViewModel : IMediaItemViewModel
	where TExecutionProgramConfigViewModel : IExecutionProgramConfigViewModel
	where TThumbnailPickerViewModel : IThumbnailPickerViewModel {
	protected readonly ConfigModel _config;
	protected readonly ITagsManager _tagsManager;

	public BaseMediaItemFactory(ConfigModel config, ITagsManager tagsManager, MediaType mediaType) {
		this._config = config;
		this._tagsManager = tagsManager;
		this.MediaType = mediaType;
	}

	public MediaType MediaType {
		get;
	}

	public abstract ItemType ItemType {
		get;
	}

	public abstract TFileOperator CreateMediaItemOperator();
	public abstract TFileModel CreateMediaItemModelFromRecord(MediaItem MediaItem);
	public abstract TFileViewModel CreateMediaItemViewModel(TFileModel fileModel);
	public abstract TThumbnailPickerViewModel CreateThumbnailPickerViewModel();

	/// <inheritdoc />
	public abstract TExecutionProgramObjectModel CreateExecutionProgramObjectModel();

	/// <inheritdoc />
	public abstract TExecutionProgramConfigViewModel CreateExecutionProgramConfigViewModel(TExecutionProgramObjectModel model);

	/// <inheritdoc />
	public abstract IBulkThumbnailConfigViewModel CreateBulkThumbnailConfigViewModel();


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

	IMediaItemOperator IMediaItemFactory.CreateMediaItemOperator() {
		return this.CreateMediaItemOperator();
	}

	IMediaItemModel IMediaItemFactory.CreateMediaItemModelFromRecord(MediaItem MediaItem) {
		return this.CreateMediaItemModelFromRecord(MediaItem);
	}

	IMediaItemViewModel IMediaItemFactory.CreateMediaItemViewModel(IMediaItemModel fileModel) {
		return this.CreateMediaItemViewModel((TFileModel)fileModel);
	}

	IThumbnailPickerViewModel IMediaItemFactory.CreateThumbnailPickerViewModel() {
		return this.CreateThumbnailPickerViewModel();
	}

	IExecutionProgramObjectModel IMediaItemFactory.CreateExecutionProgramObjectModel() {
		return this.CreateExecutionProgramObjectModel();
	}

	public IExecutionProgramConfigViewModel CreateExecutionProgramConfigViewModel(IExecutionProgramObjectModel model) {
		return this.CreateExecutionProgramConfigViewModel((TExecutionProgramObjectModel)model);
	}
}