using System.IO;



using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.FileTypes;
using MediaDeck.Composition.Interfaces.FileTypes.Models;
using MediaDeck.Composition.Interfaces.FileTypes.ViewModels;
using MediaDeck.Composition.Interfaces.FileTypes.Views;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Composition.Objects;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Database.Tables;

namespace MediaDeck.FileTypes.Base;

internal abstract class BaseFileType<TFileOperator, TFileModel, TFileViewModel, TDetailViewerPreviewControlView, TThumbnailPickerViewModel, TThumbnailPickerView> : IFileType<TFileOperator, TFileModel, TFileViewModel, TDetailViewerPreviewControlView, TThumbnailPickerViewModel, TThumbnailPickerView>
	where TFileOperator : IFileOperator
	where TFileModel : IFileModel
	where TFileViewModel : IFileViewModel
	where TDetailViewerPreviewControlView : IDetailViewerPreviewControlView
	where TThumbnailPickerViewModel : IThumbnailPickerViewModel
	where TThumbnailPickerView : IThumbnailPickerView {
	protected readonly ConfigModel _config;
	protected readonly ITagsManager _tagsManager;

	internal BaseFileType(ConfigModel config, ITagsManager tagsManager, MediaType mediaType) {
		this._config = config;
		this._tagsManager = tagsManager;
		this.MediaType = mediaType;
	}

	public MediaType MediaType {
		get;
	}

	public abstract TFileOperator CreateFileOperator();
	public abstract TFileModel CreateFileModelFromRecord(MediaFile mediaFile);
	public abstract TFileViewModel CreateFileViewModel(TFileModel fileModel);
	public abstract TDetailViewerPreviewControlView CreateDetailViewerPreviewControlView(TFileViewModel fileViewModel);
	public abstract TThumbnailPickerViewModel CreateThumbnailPickerViewModel();
	public abstract TThumbnailPickerView CreateThumbnailPickerView();
	public abstract IQueryable<MediaFile> IncludeTables(IQueryable<MediaFile> mediaFiles);

	protected void SetModelProperties(TFileModel fileModel, MediaFile mediaFile) {
		if (mediaFile.ThumbnailFileName != null) {
			fileModel.ThumbnailFilePath = Path.Combine(this._config.PathConfig.ThumbnailFolderPath.Value, mediaFile.ThumbnailFileName);
		}
		fileModel.Rate = mediaFile.Rate;
		fileModel.Description = mediaFile.Description;
		fileModel.UsageCount = mediaFile.UsageCount;
		fileModel.Exists = mediaFile.IsExists;
		fileModel.FileSize = mediaFile.FileSize;
		fileModel.Resolution = new ComparableSize(mediaFile.Width, mediaFile.Height);
		fileModel.CreationTime = mediaFile.CreationTime;
		fileModel.ModifiedTime = mediaFile.ModifiedTime;
		fileModel.LastAccessTime = mediaFile.LastAccessTime;
		fileModel.RegisteredTime = mediaFile.RegisteredTime;
		if (mediaFile.Latitude is { } lat && mediaFile.Longitude is { } lon) {
			fileModel.Location = new GpsLocation(lat, lon, mediaFile.Altitude);
		}
		fileModel.Tags = [.. mediaFile.MediaFileTags.Select(mft => this._tagsManager.Tags.FirstOrDefault(t => t.TagId == mft.TagId)).OfType<ITagModel>()];
	}

	IFileOperator IFileType.CreateFileOperator() {
		return this.CreateFileOperator();
	}

	IFileModel IFileType.CreateFileModelFromRecord(MediaFile mediaFile) {
		return this.CreateFileModelFromRecord(mediaFile);
	}

	IFileViewModel IFileType.CreateFileViewModel(IFileModel fileModel) {
		return this.CreateFileViewModel((TFileModel)fileModel);
	}

	IDetailViewerPreviewControlView IFileType.CreateDetailViewerPreviewControlView(IFileViewModel fileViewModel) {
		return this.CreateDetailViewerPreviewControlView((TFileViewModel)fileViewModel);
	}

	IThumbnailPickerViewModel IFileType.CreateThumbnailPickerViewModel() {
		return this.CreateThumbnailPickerViewModel();
	}

	IThumbnailPickerView IFileType.CreateThumbnailPickerView() {
		return this.CreateThumbnailPickerView();
	}
}