using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.MediaItemTypes.Base.Models;
using MediaDeck.MediaItemTypes.Base.ViewModels;
using MediaDeck.MediaItemTypes.Image.Models;

namespace MediaDeck.MediaItemTypes.Image.ViewModels;

/// <summary>
/// 画像用の一括サムネイル再生成設定ViewModel。固有の設定項目はない。
/// </summary>
[Inject(InjectServiceLifetime.Transient)]
public class ImageBulkThumbnailConfigViewModel : BulkThumbnailConfigViewModelBase {
	private readonly ImageMediaItemOperator _imageOperator;

	public ImageBulkThumbnailConfigViewModel(ImageMediaItemOperator imageOperator, BaseThumbnailPickerModel thumbnailPickerModel)
		: base(MediaType.Image, thumbnailPickerModel) {
		this._imageOperator = imageOperator;
	}

	protected override byte[]? GenerateThumbnail(IMediaItemViewModel target) {
		return this._imageOperator.CreateThumbnail(target.FileModel, 300, 300);
	}
}