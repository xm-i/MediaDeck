using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.FileTypes;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Database.Tables;
using MediaDeck.FileTypes.Base;
using MediaDeck.FileTypes.Image.Models;
using MediaDeck.FileTypes.Image.ViewModels;
using MediaDeck.FileTypes.Image.Views;
using Microsoft.Extensions.DependencyInjection;

namespace MediaDeck.FileTypes.Image;

[Inject(InjectServiceLifetime.Transient, typeof(IFileType))]
internal class ImageFileType : BaseFileType<ImageFileOperator, ImageFileModel, ImageFileViewModel, ImageDetailViewerPreviewControlView, ImageThumbnailPickerViewModel, ImageThumbnailPickerView> {
	private ImageDetailViewerPreviewControlView? _imageDetailViewerPreviewControlView;
	private readonly ImageFileOperator _imageFileOperator;
	private readonly IServiceProvider _serviceProvider;

	public ImageFileType(
		ImageFileOperator imageFileOperator,
		ConfigModel config,
		ITagsManager tagsManager,
		IServiceProvider serviceProvider)
		: base(config, tagsManager, MediaType.Image) {
		this._imageFileOperator = imageFileOperator;
		this._serviceProvider = serviceProvider;
	}

	public override ImageFileOperator CreateFileOperator() {
		return this._imageFileOperator;
	}

	public override ImageFileModel CreateFileModelFromRecord(MediaFile mediaFile) {
		var ifm = new ImageFileModel(mediaFile.MediaFileId, mediaFile.FilePath, this._imageFileOperator, this._config);
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
		return this._serviceProvider.GetRequiredService<ImageThumbnailPickerViewModel>();
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