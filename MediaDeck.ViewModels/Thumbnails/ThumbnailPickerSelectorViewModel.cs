using MediaDeck.Common.Base;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;

namespace MediaDeck.ViewModels.Thumbnails;

[Inject(InjectServiceLifetime.Transient)]
public class ThumbnailPickerSelectorViewModel : ViewModelBase {
	/// <summary>
	/// <see cref="ThumbnailPickerSelectorViewModel"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="MediaItemTypeService">メディアアイテム種別サービス</param>
	public ThumbnailPickerSelectorViewModel(IMediaItemTypeService MediaItemTypeService) {
		this.ThumbnailPickerViewModel = new BindableReactiveProperty<IThumbnailPickerViewModel?>().AddTo(this.CompositeDisposable);
		this.FileViewModel = new BindableReactiveProperty<IMediaItemViewModel>().AddTo(this.CompositeDisposable);

		this.FileViewModel.SubscribeAwait(async (x, ct) => {
			(this.ThumbnailPickerViewModel.Value as IDisposable)?.Dispose();
			if (x == null) {
				this.ThumbnailPickerViewModel.Value = null;
				return;
			}
			var thumbnailPickerViewModel = MediaItemTypeService.CreateThumbnailPickerViewModel(x);
			await thumbnailPickerViewModel.LoadAsync(x);
			this.ThumbnailPickerViewModel.Value = thumbnailPickerViewModel;
		}, AwaitOperation.Switch).AddTo(this.CompositeDisposable);
	}

	/// <summary>
	/// サムネイルピッカーのViewModel
	/// </summary>
	public BindableReactiveProperty<IThumbnailPickerViewModel?> ThumbnailPickerViewModel {
		get;
	}

	/// <summary>
	/// 対象ファイルのViewModel
	/// </summary>
	public BindableReactiveProperty<IMediaItemViewModel> FileViewModel {
		get;
	}

	/// <inheritdoc/>
	protected override void Dispose(bool disposing) {
		if (disposing) {
			(this.ThumbnailPickerViewModel.Value as IDisposable)?.Dispose();
		}
		base.Dispose(disposing);
	}
}