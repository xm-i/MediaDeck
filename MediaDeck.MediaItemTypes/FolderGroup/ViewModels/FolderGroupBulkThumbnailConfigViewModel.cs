using System.Threading;
using System.Threading.Tasks;

using MediaDeck.Common.Base;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.MediaItemTypes.Base.Models;
using MediaDeck.MediaItemTypes.FolderGroup.Models;

namespace MediaDeck.MediaItemTypes.FolderGroup.ViewModels;

/// <summary>
/// フォルダグループ用の一括サムネイル再生成設定ViewModel。
/// フォルダ内の N 番目のアイテムのサムネイルを代表として複製する。
/// </summary>
[Inject(InjectServiceLifetime.Transient)]
public class FolderGroupBulkThumbnailConfigViewModel : ViewModelBase, IBulkThumbnailConfigViewModel {
	private readonly FolderGroupThumbnailPickerModel _model;
	private readonly BaseThumbnailPickerModel _baseThumbnailPickerModel;
	private readonly IFilePathService _filePathService;

	public FolderGroupBulkThumbnailConfigViewModel(FolderGroupThumbnailPickerModel model, BaseThumbnailPickerModel baseThumbnailPickerModel, IFilePathService filePathService) {
		this._model = model;
		this._baseThumbnailPickerModel = baseThumbnailPickerModel;
		this._filePathService = filePathService;
	}

	public MediaType MediaType {
		get;
	} = MediaType.FolderGroup;

	/// <summary>
	/// フォルダ内アイテムの何番目 (1 始まり) のサムネイルを使うか。
	/// </summary>
	public BindableReactiveProperty<int> ItemIndex {
		get;
	} = new(1);

	public async Task ApplyToAsync(IMediaItemViewModel target, CancellationToken cancellationToken) {
		cancellationToken.ThrowIfCancellationRequested();
		await this._model.LoadItemsInFolderAsync(target.FilePath);
		cancellationToken.ThrowIfCancellationRequested();
		if (this._model.Items.Count == 0) {
			return;
		}
		var index = Math.Clamp(this.ItemIndex.Value - 1, 0, this._model.Items.Count - 1);
		var representative = this._model.Items[index];
		var bytes = this._model.GetThumbnailBinary(representative.FileModel);
		if (bytes is null) {
			return;
		}
		cancellationToken.ThrowIfCancellationRequested();
		await this._baseThumbnailPickerModel.UpdateThumbnailAsync(target.FileModel, bytes);
		target.RefreshThumbnail();
	}
}