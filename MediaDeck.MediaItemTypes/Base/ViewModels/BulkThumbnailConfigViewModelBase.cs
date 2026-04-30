using System.Threading;
using System.Threading.Tasks;

using MediaDeck.Common.Base;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.MediaItemTypes.Base.Models;

namespace MediaDeck.MediaItemTypes.Base.ViewModels;

/// <summary>
/// 一括サムネイル再生成用の設定ViewModel基底クラス。
/// 派生クラスは <see cref="GenerateThumbnailAsync"/> でメディアタイプ固有の生成処理を実装する。
/// </summary>
public abstract class BulkThumbnailConfigViewModelBase : ViewModelBase, IBulkThumbnailConfigViewModel {
	private readonly BaseThumbnailPickerModel _thumbnailPickerModel;

	protected BulkThumbnailConfigViewModelBase(MediaType mediaType, BaseThumbnailPickerModel thumbnailPickerModel) {
		this.MediaType = mediaType;
		this._thumbnailPickerModel = thumbnailPickerModel;
	}

	public MediaType MediaType {
		get;
	}

	public async Task ApplyToAsync(IMediaItemViewModel target, CancellationToken cancellationToken) {
		cancellationToken.ThrowIfCancellationRequested();
		var bytes = await Task.Run(() => this.GenerateThumbnail(target), cancellationToken);
		cancellationToken.ThrowIfCancellationRequested();
		if (bytes is null) {
			return;
		}
		await this._thumbnailPickerModel.UpdateThumbnailAsync(target.FileModel, bytes);
		target.RefreshThumbnail();
	}

	/// <summary>
	/// 派生クラスでサムネイルバイナリを生成する。
	/// </summary>
	protected abstract byte[]? GenerateThumbnail(IMediaItemViewModel target);
}