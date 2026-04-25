using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Database.Tables;
using MediaDeck.MediaItemTypes.Base;
using MediaDeck.MediaItemTypes.Image.Models;
using MediaDeck.MediaItemTypes.Image.ViewModels;
using MediaDeck.MediaItemTypes.Image.Views;
using Microsoft.Extensions.DependencyInjection;

namespace MediaDeck.MediaItemTypes.Image;

[Inject(InjectServiceLifetime.Singleton, typeof(IMediaItemType))]
internal class ImageMediaItemType : BaseMediaItemType<ImageMediaItemOperator, ImageMediaItemModel, ImageMediaItemViewModel, ImageDetailViewerPreviewControlView, ImageThumbnailPickerViewModel, ImageThumbnailPickerView> {
	private ImageDetailViewerPreviewControlView? _imageDetailViewerPreviewControlView;
	private readonly ImageMediaItemOperator _ImageMediaItemOperator;
	private readonly IServiceProvider _serviceProvider;

	public ImageMediaItemType(
		ImageMediaItemOperator ImageMediaItemOperator,
		ConfigModel config,
		ITagsManager tagsManager,
		IServiceProvider serviceProvider)
		: base(config, tagsManager, MediaType.Image) {
		this._ImageMediaItemOperator = ImageMediaItemOperator;
		this._serviceProvider = serviceProvider;
	}

	public override ImageMediaItemOperator CreateMediaItemOperator() {
		return this._ImageMediaItemOperator;
	}

	public override ItemType ItemType {
		get {
			return MediaDeck.Database.Tables.ItemType.Image;
		}
	}

	public override ImageMediaItemModel CreateMediaItemModelFromRecord(MediaItem MediaItem) {
		var ifm = new ImageMediaItemModel(MediaItem.MediaItemId, MediaItem.FilePath, this._ImageMediaItemOperator, this._config);
		this.SetModelProperties(ifm, MediaItem);
		return ifm;
	}

	public override ImageMediaItemViewModel CreateMediaItemViewModel(ImageMediaItemModel fileModel) {
		return new ImageMediaItemViewModel(fileModel);
	}

	public override ImageDetailViewerPreviewControlView CreateDetailViewerPreviewControlView(ImageMediaItemViewModel fileViewModel) {
		return this._imageDetailViewerPreviewControlView ??= new ImageDetailViewerPreviewControlView();
	}

	public override ImageThumbnailPickerViewModel CreateThumbnailPickerViewModel() {
		return this._serviceProvider.GetRequiredService<ImageThumbnailPickerViewModel>();
	}

	public override ImageThumbnailPickerView CreateThumbnailPickerView() {
		return new ImageThumbnailPickerView();
	}

	public override IQueryable<MediaItem> IncludeTables(IQueryable<MediaItem> MediaItems) {
		return MediaItems
			.Include(mf => mf.ImageFile)
			.Include(mf => mf.Jpeg)
			.Include(mf => mf.Png)
			.Include(mf => mf.Bmp)
			.Include(mf => mf.Gif)
			.Include(mf => mf.Heif);
	}
}