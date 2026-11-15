using System.Threading.Tasks;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.MediaItemTypes.Base.ViewModels;
using MediaDeck.MediaItemTypes.FolderGroup.Models;

namespace MediaDeck.MediaItemTypes.FolderGroup.ViewModels;

/// <summary>
/// フォルダグループのサムネイルピッカーViewModel
/// </summary>
[Inject(InjectServiceLifetime.Transient)]
public class FolderGroupThumbnailPickerViewModel : BaseThumbnailPickerViewModel<FolderGroupThumbnailPickerModel> {
	public FolderGroupThumbnailPickerViewModel(FolderGroupThumbnailPickerModel thumbnailPickerModel, IFilePickerService filePickerService, ConfigModel config) : base(thumbnailPickerModel, filePickerService, config) {

		// UIスレッドへの同期を伴うビューリストを作成
		this.Items = this.thumbnailPickerModel.Items.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current).AddTo(this.CompositeDisposable);

		this.SelectedItem.SubscribeAwait(async (x, ct) => {
			if (x != null) {
				await this.RecreateThumbnailAsync();
			} else {
				this.CandidateThumbnail.Value = null;
			}
		}, AwaitOperation.Drop).AddTo(this.CompositeDisposable);
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

	public override async Task RecreateThumbnailAsync() {
		if (this.SelectedItem.Value == null) {
			this.CandidateThumbnail.Value = null;
			return;
		}

		var fileModel = this.SelectedItem.Value.FileModel;
		this.CandidateThumbnail.Value = await Task.Run(() => this.thumbnailPickerModel.GetThumbnailBinary(fileModel));
	}
}