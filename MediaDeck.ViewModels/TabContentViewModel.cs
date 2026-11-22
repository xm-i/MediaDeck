using MediaDeck.Common.Base;
using MediaDeck.Common.Extensions;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.ViewModels.Panes.DetailPanes;
using MediaDeck.ViewModels.Panes.FilterPanes;
using MediaDeck.ViewModels.Panes.RepositoryPanes;
using MediaDeck.ViewModels.Panes.SortPanes;
using MediaDeck.ViewModels.Panes.StatusPanes;
using MediaDeck.ViewModels.Panes.ViewerPanes;

namespace MediaDeck.ViewModels;

/// <summary>
/// 1つのタブのコンテンツ本体に対応するViewModel。
/// 初回アクティブ化時にタブ固有のDIスコープから解決される。
/// </summary>
[Inject(InjectServiceLifetime.Scoped)]
public class TabContentViewModel : ViewModelBase {
	/// <summary>
	/// このタブの状態モデルを取得します。
	/// </summary>
	public TabStateModel TabState {
		get;
	}

	/// <summary>
	/// ビューアーセレクターViewModelを取得します。
	/// </summary>
	public ViewerSelectorViewModel ViewerSelectorViewModel {
		get;
	}

	/// <summary>
	/// フィルターセレクターViewModelを取得します。
	/// </summary>
	public FilterSelectorViewModel FilterSelectorViewModel {
		get;
	}

	/// <summary>
	/// ソートセレクターViewModelを取得します。
	/// </summary>
	public SortSelectorViewModel SortSelectorViewModel {
		get;
	}

	/// <summary>
	/// 詳細セレクターViewModelを取得します。
	/// </summary>
	public DetailSelectorViewModel DetailSelectorViewModel {
		get;
	}

	/// <summary>
	/// リポジトリセレクターViewModelを取得します。
	/// </summary>
	public RepositorySelectorViewModel RepositorySelectorViewModel {
		get;
	}

	/// <summary>
	/// ステータスバーViewModelを取得します。
	/// </summary>
	public StatusBarViewModel StatusBarViewModel {
		get;
	}

	/// <summary>
	/// 左ペインの幅を取得します。
	/// </summary>
	public BindableReactiveProperty<double> LeftPaneWidth {
		get;
	}

	/// <summary>
	/// 右ペインの幅を取得します。
	/// </summary>
	public BindableReactiveProperty<double> RightPaneWidth {
		get;
	}

	/// <summary>
	/// リポジトリペインの高さを取得します。
	/// </summary>
	public BindableReactiveProperty<double> RepositoryPaneHeight {
		get;
	}

	/// <summary>
	/// <see cref="TabContentViewModel"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="tabState">タブ状態モデル。</param>
	/// <param name="viewerSelectorViewModel">ビューアーセレクターViewModel。</param>
	/// <param name="filterSelectorViewModel">フィルターセレクターViewModel。</param>
	/// <param name="sortSelectorViewModel">ソートセレクターViewModel。</param>
	/// <param name="detailSelectorViewModel">詳細セレクターViewModel。</param>
	/// <param name="repositorySelectorViewModel">リポジトリセレクターViewModel。</param>
	/// <param name="statusBarViewModel">ステータスバーViewModel。</param>
	public TabContentViewModel(
		TabStateModel tabState,
		ViewerSelectorViewModel viewerSelectorViewModel,
		FilterSelectorViewModel filterSelectorViewModel,
		SortSelectorViewModel sortSelectorViewModel,
		DetailSelectorViewModel detailSelectorViewModel,
		RepositorySelectorViewModel repositorySelectorViewModel,
		StatusBarViewModel statusBarViewModel) {
		this.TabState = tabState;

		this.LeftPaneWidth = this.TabState.LeftPaneWidth.ToTwoWayBindableReactiveProperty(this.TabState.LeftPaneWidth.Value, this.CompositeDisposable);
		this.RightPaneWidth = this.TabState.RightPaneWidth.ToTwoWayBindableReactiveProperty(this.TabState.RightPaneWidth.Value, this.CompositeDisposable);
		this.RepositoryPaneHeight = this.TabState.RepositoryPaneHeight.ToTwoWayBindableReactiveProperty(this.TabState.RepositoryPaneHeight.Value, this.CompositeDisposable);

		this.ViewerSelectorViewModel = viewerSelectorViewModel;
		this.FilterSelectorViewModel = filterSelectorViewModel;
		this.SortSelectorViewModel = sortSelectorViewModel;
		this.DetailSelectorViewModel = detailSelectorViewModel;
		this.RepositorySelectorViewModel = repositorySelectorViewModel;
		this.StatusBarViewModel = statusBarViewModel.AddTo(this.CompositeDisposable);

		this.ViewerSelectorViewModel.MediaContentLibraryViewModel.SelectedFiles.Subscribe(x => {
			this.DetailSelectorViewModel.TargetFiles.Value = x.Select(v => v.FileModel).ToArray();
		}).AddTo(this.CompositeDisposable);
	}

	/// <summary>
	/// メディアライブラリのデータを再読み込みします。
	/// </summary>
	public virtual void Reload() {
		this.ViewerSelectorViewModel.MediaContentLibraryViewModel.Reload();
	}
}