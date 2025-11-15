using CommunityToolkit.Mvvm.DependencyInjection;

using MediaDeck.Database.Tables;
using MediaDeck.FileTypes.Base;
using MediaDeck.FileTypes.Image.Models;
using MediaDeck.FileTypes.Image.ViewModels;
using MediaDeck.FileTypes.Image.Views;
using MediaDeck.Utils.Enums;

namespace MediaDeck.FileTypes.Image;
[AddTransient(typeof(IFileType))]
public class ImageFileType : BaseFileType<ImageFileOperator, ImageFileModel, ImageFileViewModel, ImageDetailViewerPreviewControlView, ImageThumbnailPickerViewModel, ImageThumbnailPickerView> {
	private ImageDetailViewerPreviewControlView? _imageDetailViewerPreviewControlView;
	public override MediaType MediaType {
		get;
	} = MediaType.Image;

	public override ImageFileOperator CreateFileOperator() {
		return new ImageFileOperator();
	}

	public override ImageFileModel CreateFileModelFromRecord(MediaFile mediaFile) {
		var ifm = new ImageFileModel(mediaFile.MediaFileId, mediaFile.FilePath);
		this.SetModelProperties(ifm, mediaFile);
		return ifm;
	}

	public override ImageFileViewModel CreateFileViewModel(ImageFileModel fileModel) {
		return new ImageFileViewModel(fileModel);
	}
	public override ImageDetailViewerPreviewControlView CreateDetailViewerPreviewControlView(ImageFileViewModel fileViewModel) {
		return this._imageDetailViewerPreviewControlView ??= new ImageDetailViewerPreviewControlView();
	}

	public override ImageThumbnailPickerViewModel CreateThumbnailPickerViewModel() {
		return Ioc.Default.GetRequiredService<ImageThumbnailPickerViewModel>();
	}

	public override ImageThumbnailPickerView CreateThumbnailPickerView() {
		return new ImageThumbnailPickerView();
	}

	public override IQueryable<MediaFile> IncludeTables(IQueryable<MediaFile> mediaFiles) {
		return mediaFiles
			.Include(mf => mf.ImageFile)
			.Include(mf => mf.Jpeg)
			.Include(mf => mf.Png)
			.Include(mf => mf.Bmp)
			.Include(mf => mf.Gif)
			.Include(mf => mf.Heif);
	}
}
