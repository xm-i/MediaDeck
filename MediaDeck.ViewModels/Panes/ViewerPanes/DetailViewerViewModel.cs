using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Core.Models.Files;

namespace MediaDeck.ViewModels.Panes.ViewerPanes;

/// <summary>
/// 単一メディアの詳細情報・プレビューを表示するビューの ViewModel。
/// </summary>
[Inject(InjectServiceLifetime.Transient)]
public class DetailViewerViewModel : ViewerPaneViewModelBase, IDetailViewerViewModel {
	/// <summary>
	/// <see cref="DetailViewerViewModel"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="filesManager">ファイルマネージャ。</param>
	/// <param name="mediaContentLibraryViewModel">メディアライブラリ ViewModel。</param>
	/// <param name="searchConditionManagerViewModel">検索条件マネージャ ViewModel。</param>
	/// <param name="tabState">タブ状態モデル。</param>
	public DetailViewerViewModel(
		FilesManager filesManager,
		MediaContentLibraryViewModel mediaContentLibraryViewModel,
		SearchConditionManagerViewModel searchConditionManagerViewModel,
		TabStateModel tabState)
		: base(ViewerType.Detail, "Detail", "\uE954", filesManager, mediaContentLibraryViewModel, searchConditionManagerViewModel, tabState) {
		mediaContentLibraryViewModel.SelectedFile.Subscribe(x => {
			this.SelectedFile.Value = x;
			this.SelectedFilePath.Value = x?.FilePath;
			this.SelectedFileThumbnailFilePath.Value = x?.ThumbnailFilePath.Value;
		}).AddTo(this.CompositeDisposable);
	}

	/// <inheritdoc/>
	public BindableReactiveProperty<IMediaItemViewModel?> SelectedFile {
		get;
	} = new(null);

	/// <inheritdoc/>
	public BindableReactiveProperty<string?> SelectedFilePath {
		get;
	} = new(null);

	/// <inheritdoc/>
	public BindableReactiveProperty<string?> SelectedFileThumbnailFilePath {
		get;
	} = new(null);
}