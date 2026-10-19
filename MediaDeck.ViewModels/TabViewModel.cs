using MediaDeck.Common.Base;
using MediaDeck.Common.Extensions;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Core.Stores.State;
using MediaDeck.ViewModels.Panes.DetailPanes;
using MediaDeck.ViewModels.Panes.FilterPanes;
using MediaDeck.ViewModels.Panes.RepositoryPanes;
using MediaDeck.ViewModels.Panes.SortPanes;
using MediaDeck.ViewModels.Panes.StatusPanes;
using MediaDeck.ViewModels.Panes.ViewerPanes;

namespace MediaDeck.ViewModels;

/// <summary>
/// 1つのタブに対応するViewModel。
/// タブ固有のDIスコープ内で管理される。
/// </summary>
[Inject(InjectServiceLifetime.Scoped)]
public class TabViewModel : ViewModelBase {
	/// <summary>
	/// このタブの状態モデル
	/// </summary>
	public TabStateModel TabState {
		get;
	}

	/// <summary>
	/// タブの表示名
	/// </summary>
	public BindableReactiveProperty<string> DisplayName {
		get;
	}

	public ViewerSelectorViewModel ViewerSelectorViewModel {
		get;
	}

	public FilterSelectorViewModel FilterSelectorViewModel {
		get;
	}

	public DetailSelectorViewModel DetailSelectorViewModel {
		get;
	}

	public RepositorySelectorViewModel RepositorySelectorViewModel {
		get;
	}

	public StatusBarViewModel StatusBarViewModel {
		get;
	}

	public SortSelectorViewModel SortSelectorViewModel {
		get;
	}

	public BindableReactiveProperty<double> LeftPaneWidth {
		get;
	}

	public BindableReactiveProperty<double> RightPaneWidth {
		get;
	}

	public BindableReactiveProperty<double> RepositoryPaneHeight {
		get;
	}

	public TabViewModel(
		TabStateModel tabState,
		IStateStore stateStore,
		ViewerSelectorViewModel viewerSelectorViewModel,
		FilterSelectorViewModel filterSelectorViewModel,
		SortSelectorViewModel sortSelectorViewModel,
		DetailSelectorViewModel detailSelectorViewModel,
		RepositorySelectorViewModel repositorySelectorViewModel,
		StatusBarViewModel statusBarViewModel) {
		this.TabState = tabState;

		this.DisplayName = this.TabState.DisplayName.ToBindableReactiveProperty("New Tab").AddTo(this.CompositeDisposable);
		this.LeftPaneWidth = this.TabState.LeftPaneWidth.ToTwoWayBindableReactiveProperty().AddTo(this.CompositeDisposable);
		this.RightPaneWidth = this.TabState.RightPaneWidth.ToTwoWayBindableReactiveProperty().AddTo(this.CompositeDisposable);
		this.RepositoryPaneHeight = this.TabState.RepositoryPaneHeight.ToTwoWayBindableReactiveProperty().AddTo(this.CompositeDisposable);

		this.ViewerSelectorViewModel = viewerSelectorViewModel;
		this.FilterSelectorViewModel = filterSelectorViewModel;
		this.SortSelectorViewModel = sortSelectorViewModel;
		this.DetailSelectorViewModel = detailSelectorViewModel;
		this.RepositorySelectorViewModel = repositorySelectorViewModel;
		this.StatusBarViewModel = statusBarViewModel.AddTo(this.CompositeDisposable);

		this.ViewerSelectorViewModel.MediaContentLibraryViewModel.SelectedFiles.Subscribe(x => {
			this.DetailSelectorViewModel.TargetFiles.Value = x.Select(v => v.FileModel).ToArray();
		}).AddTo(this.CompositeDisposable);

		// タブの状態変更をAppStateのデフォルトタブ状態に同期
		this.SubscribeDefaultTabStateSync(stateStore.RootState.AppState);

		// 初回ロード
		this.ViewerSelectorViewModel.MediaContentLibraryViewModel.Reload();
	}

	/// <summary>
	/// タブの状態変更をAppStateのデフォルトタブ状態に同期する
	/// </summary>
	private void SubscribeDefaultTabStateSync(AppStateModel appState) {
		var defaultSearch = appState.DefaultTabState.SearchState;
		var defaultViewer = appState.DefaultTabState.ViewerState;

		// SearchState の同期
		this.TabState.SearchState.CurrentSortCondition.Skip(1).Subscribe(v => defaultSearch.CurrentSortCondition.Value = v).AddTo(this.CompositeDisposable);
		this.TabState.SearchState.SortDirection.Skip(1).Subscribe(v => defaultSearch.SortDirection.Value = v).AddTo(this.CompositeDisposable);
		this.TabState.SearchState.CurrentFilteringConditions.Skip(1).Subscribe(v => defaultSearch.CurrentFilteringConditions.Value = v).AddTo(this.CompositeDisposable);

		// ViewerState の同期
		this.TabState.ViewerState.ItemSize.Skip(1).Subscribe(v => defaultViewer.ItemSize.Value = v).AddTo(this.CompositeDisposable);
		this.TabState.ViewerState.ActiveViewer.Skip(1).Subscribe(v => defaultViewer.ActiveViewer.Value = v).AddTo(this.CompositeDisposable);
		this.TabState.ViewerState.ShowOverlay.Skip(1).Subscribe(v => defaultViewer.ShowOverlay.Value = v).AddTo(this.CompositeDisposable);
		this.TabState.ViewerState.ShowInfo.Skip(1).Subscribe(v => defaultViewer.ShowInfo.Value = v).AddTo(this.CompositeDisposable);

		// ListViewer 列設定の同期
		void SyncColumn(ListViewerColumnStateModel col) {
			var defaultCol = defaultViewer.GetOrCreateColumnState(col.ColumnId, col.Width.Value, col.IsVisible.Value);
			col.Width.Skip(1).Subscribe(v => defaultCol.Width.Value = v).AddTo(this.CompositeDisposable);
			col.IsVisible.Skip(1).Subscribe(v => defaultCol.IsVisible.Value = v).AddTo(this.CompositeDisposable);
		}

		foreach (var col in this.TabState.ViewerState.ListColumns) {
			SyncColumn(col);
		}
		this.TabState.ViewerState.ListColumns.ObserveAdd()
			.Subscribe(e => SyncColumn(e.Value))
			.AddTo(this.CompositeDisposable);

		// リポジトリの同期
		this.TabState.ActiveRepository.Skip(1).Subscribe(v => appState.DefaultTabState.ActiveRepository.Value = v).AddTo(this.CompositeDisposable);
		// Splitterの同期
		this.TabState.LeftPaneWidth.Skip(1).Subscribe(v => appState.DefaultTabState.LeftPaneWidth.Value = v).AddTo(this.CompositeDisposable);
		this.TabState.RightPaneWidth.Skip(1).Subscribe(v => appState.DefaultTabState.RightPaneWidth.Value = v).AddTo(this.CompositeDisposable);
		this.TabState.RepositoryPaneHeight.Skip(1).Subscribe(v => appState.DefaultTabState.RepositoryPaneHeight.Value = v).AddTo(this.CompositeDisposable);
	}
}