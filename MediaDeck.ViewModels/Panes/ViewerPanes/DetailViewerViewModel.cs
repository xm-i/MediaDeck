using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Composition.Stores.Config.Model;
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
	/// <param name="executionConfig">外部実行設定モデル。</param>
	public DetailViewerViewModel(
		FilesManager filesManager,
		MediaContentLibraryViewModel mediaContentLibraryViewModel,
		SearchConditionManagerViewModel searchConditionManagerViewModel,
		TabStateModel tabState,
		ExecutionConfigModel executionConfig)
		: base(ViewerType.Detail, "Detail", "\uE954", filesManager, mediaContentLibraryViewModel, searchConditionManagerViewModel, tabState, executionConfig) {
		this.SelectedFile = new BindableReactiveProperty<IMediaItemViewModel?>(null).AddTo(this.CompositeDisposable);
		this.SelectedFilePath = new BindableReactiveProperty<string?>(null).AddTo(this.CompositeDisposable);
		this.SelectedFileThumbnailFilePath = new BindableReactiveProperty<string?>(null).AddTo(this.CompositeDisposable);

		mediaContentLibraryViewModel.SelectedFile.Subscribe(x => {
			this.SelectedFile.Value = x;
			this.SelectedFilePath.Value = x?.FilePath;
			this.SelectedFileThumbnailFilePath.Value = x?.ThumbnailFilePath.Value;
		}).AddTo(this.CompositeDisposable);
	}

	/// <inheritdoc/>
	public BindableReactiveProperty<IMediaItemViewModel?> SelectedFile {
		get;
	}

	/// <inheritdoc/>
	public BindableReactiveProperty<string?> SelectedFilePath {
		get;
	}

	/// <inheritdoc/>
	public BindableReactiveProperty<string?> SelectedFileThumbnailFilePath {
		get;
	}
}