using System.Threading.Tasks;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.MediaItemTypes.Base.ViewModels;
using MediaDeck.MediaItemTypes.FolderGroup.Models;

namespace MediaDeck.MediaItemTypes.FolderGroup.ViewModels;

/// <summary>
/// フォルダグループのサムネイルピッカーViewModel
/// </summary>
[Inject(InjectServiceLifetime.Transient)]
public class FolderGroupThumbnailPickerViewModel : BaseThumbnailPickerViewModel<FolderGroupThumbnailPickerModel> {
	public FolderGroupThumbnailPickerViewModel(FolderGroupThumbnailPickerModel thumbnailPickerModel) : base(thumbnailPickerModel) {

		// UIスレッドへの同期を伴うビューリストを作成
		this.Items = this.thumbnailPickerModel.Items.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);

		this.SelectedItem.Subscribe(x => {
			if (x != null) {
				this.RecreateThumbnail();
			} else {
				this.CandidateThumbnail.Value = null;
			}
		}).AddTo(this.CompositeDisposable);
	}

	/// <summary>
	/// WinUIバインディング用のアイテムリスト
	/// </summary>
	public INotifyCollectionChangedSynchronizedViewList<IMediaItemViewModel> Items {
		get;
	}

	/// <summary>
	/// 選択されたアイテム
	/// </summary>
	public BindableReactiveProperty<IMediaItemViewModel?> SelectedItem { get; } = new();

	public override async Task LoadAsync(IMediaItemViewModel fileViewModel) {
		await base.LoadAsync(fileViewModel);
		await this.thumbnailPickerModel.LoadItemsInFolderAsync(fileViewModel.FilePath);

	}

	public override void RecreateThumbnail() {
		if (this.SelectedItem.Value == null) {
			this.CandidateThumbnail.Value = null;
			return;
		}

		this.CandidateThumbnail.Value = this.thumbnailPickerModel.GetThumbnailBinary(this.SelectedItem.Value.FileModel);
	}
}