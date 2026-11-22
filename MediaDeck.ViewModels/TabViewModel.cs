using MediaDeck.Common.Base;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Core.Stores.State;

namespace MediaDeck.ViewModels;

/// <summary>
/// 1つのタブに対応する軽量シェルViewModel。
/// 実体となる <see cref="TabContentViewModel"/> は初回アクティブ化時に遅延生成される。
/// </summary>
public class TabViewModel : ViewModelBase {
	private readonly IStateStore _stateStore;
	private readonly Func<TabContentViewModel> _contentFactory;
	private bool _isLoaded;

	/// <summary>
	/// このタブの状態モデルを取得します。
	/// </summary>
	public TabStateModel TabState {
		get;
	}

	/// <summary>
	/// タブの表示名を取得します。
	/// </summary>
	public BindableReactiveProperty<string> DisplayName {
		get;
	}

	/// <summary>
	/// 遅延ロードされるタブコンテンツ本体を取得します。未ロード時は <c>null</c> です。
	/// </summary>
	public BindableReactiveProperty<TabContentViewModel?> Content {
		get;
	}

	/// <summary>
	/// タブコンテンツがロード済みであるかどうかを取得します。
	/// </summary>
	public bool IsLoaded {
		get {
			return this.Content.Value != null;
		}
	}

	/// <summary>
	/// <see cref="TabViewModel"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="tabState">タブ状態モデル。</param>
	/// <param name="stateStore">アプリケーション状態ストア。</param>
	/// <param name="contentFactory">タブコンテンツ本体を生成するファクトリデリゲート。</param>
	public TabViewModel(
		TabStateModel tabState,
		IStateStore stateStore,
		Func<TabContentViewModel> contentFactory) {
		this.TabState = tabState;
		this._stateStore = stateStore;
		this._contentFactory = contentFactory;

		this.DisplayName = this.TabState.DisplayName.ToBindableReactiveProperty("New Tab").AddTo(this.CompositeDisposable);
		this.Content = new BindableReactiveProperty<TabContentViewModel?>(null).AddTo(this.CompositeDisposable);
	}

	/// <summary>
	/// タブコンテンツが未ロードの場合に生成・初期化および初回データロードを実行します。
	/// 既にロード済みまたは破棄済みの場合は何もしません。
	/// </summary>
	public void EnsureLoaded() {
		if (this.DisposeState != DisposeState.NotDisposed || this._isLoaded) {
			return;
		}

		this._isLoaded = true;
		try {
			var content = this._contentFactory();
			content.AddTo(this.CompositeDisposable);
			this.SubscribeDefaultTabStateSync(this._stateStore.RootState.AppState);
			this.Content.Value = content;
			content.Reload();
		} catch {
			this._isLoaded = false;
			throw;
		}
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