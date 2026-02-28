using System.IO;

using CommunityToolkit.Mvvm.DependencyInjection;

using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Composition.Interfaces.FileTypes.Models;
using MediaDeck.Composition.Objects;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Database.Tables;
using MediaDeck.FileTypes.Base.Models.Interfaces;
using MediaDeck.FileTypes.Base.ViewModels.Interfaces;
using MediaDeck.FileTypes.Base.Views.Interfaces;
using MediaDeck.Models.Files;

namespace MediaDeck.FileTypes.Base;
public abstract class BaseFileType<TFileOperator, TFileModel, TFileViewModel, TDetailViewerPreviewControlView, TThumbnailPickerViewModel, TThumbnailPickerView> : IFileType<TFileOperator, TFileModel, TFileViewModel, TDetailViewerPreviewControlView, TThumbnailPickerViewModel, TThumbnailPickerView>
	where TFileOperator : IFileOperator
	where TFileModel : IFileModel
	where TFileViewModel : IFileViewModel
	where TDetailViewerPreviewControlView: IDetailViewerPreviewControlView
	where TThumbnailPickerViewModel : IThumbnailPickerViewModel
	where TThumbnailPickerView : IThumbnailPickerView {
	public abstract MediaType MediaType {
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
			fileModel.ThumbnailFilePath = Path.Combine(Ioc.Default.GetRequiredService<ConfigModel>().PathConfig.ThumbnailFolderPath.Value, mediaFile.ThumbnailFileName);
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
		if (mediaFile.Latitude is { } lat && mediaFile.Longitude is { } lon ) {
			fileModel.Location = new GpsLocation(lat, lon, mediaFile.Altitude);
		}
		fileModel.Tags = [.. mediaFile.MediaFileTags.Select(mft => new TagModel(mft.Tag) as ITagModel)];
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
