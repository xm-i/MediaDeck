using System.Threading.Tasks;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Composition.Stores.Config.Model;
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

	public ImageBulkThumbnailConfigViewModel(ImageMediaItemOperator imageOperator, BaseThumbnailPickerModel thumbnailPickerModel, ConfigModel config)
		: base(MediaType.Image, thumbnailPickerModel, config) {
		this._imageOperator = imageOperator;
	}

	protected override async Task<byte[]?> GenerateThumbnailAsync(IMediaItemViewModel target) {
		return await Task.Run(() => this._imageOperator.CreateThumbnail(target.FileModel, (uint)this._config.ThumbnailConfig.ThumbnailSize.Value, (uint)this._config.ThumbnailConfig.ThumbnailSize.Value));
	}
}