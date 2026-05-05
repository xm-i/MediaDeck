using MediaDeck.Common.Base;
using MediaDeck.Common.Extensions;
using MediaDeck.Composition.Objects;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Core.Stores.State;
using MediaDeck.ViewModels.Tools;

using Microsoft.Extensions.DependencyInjection;

namespace MediaDeck.ViewModels;

[Inject(InjectServiceLifetime.Scoped)]
public class MainWindowViewModel : ViewModelBase {
	private readonly IServiceProvider _rootServiceProvider;
	private readonly WindowStateModel _windowState;
	private readonly RootStateModel _rootState;


	public MainWindowViewModel(
		IServiceProvider serviceProvider,
		IStateStore stateStore,
		WindowStateProvider windowStateProvider,
		BackgroundTasksViewModel backgroundTasksViewModel,
		NavigationMenuViewModel navigationMenuViewModel) {
		this._rootServiceProvider = serviceProvider;
		this.NavigationMenuViewModel = navigationMenuViewModel;
		this._rootState = stateStore.RootState;

		// 自身のウィンドウの状態を取得（WindowManagerによってセット済み）
		this._windowState = windowStateProvider.State ?? throw new InvalidOperationException("WindowStateModel was not provided to the scope.");

		this.Tabs = this._windowState.TabIds.ToWritableNotifyCollectionChanged(
				tabId => {
					var tabState = this._rootState.Tabs.FirstOrDefault(t => t.TabId == tabId)
						?? throw new InvalidOperationException($"TabStateModel not found for TabId: {tabId}");
					return new TabContext(tabState);
				},
			(TabContext tabContext, Guid tabId, ref bool setValue) => {
				setValue = true;
				return tabContext.TabState.TabId;
			},
			SynchronizationContextCollectionEventDispatcher.Current);

		this.SelectedTab = this._windowState.SelectedTabId.ToTwoWayBindableReactiveProperty(
			x => x.HasValue ? this.Tabs.FirstOrDefault(t => t.TabState.TabId == x.Value) : null,
			x => x?.TabState.TabId,
			null,
			this.CompositeDisposable
			);

		this.AddTabCommand.Subscribe(x => {
			this.AddTab();
		}).AddTo(this.CompositeDisposable);

		this.CloseTabCommand.Subscribe(this.CloseTab).AddTo(this.CompositeDisposable);
	}

	/// <summary>
	/// タブ一覧
	/// </summary>
	public INotifyCollectionChangedSynchronizedViewList<TabContext> Tabs {
		get;
	}

	/// <summary>
	/// 選択中のタブ
	/// </summary>
	public BindableReactiveProperty<TabContext?> SelectedTab {
		get;
	}

	public NavigationMenuViewModel NavigationMenuViewModel {
		get;
	}

	public ReactiveCommand WindowActivatedCommand {
		get;
	} = new();

	public ReactiveCommand AddTabCommand {
		get;
	} = new();

	public ReactiveCommand<TabContext> CloseTabCommand {
		get;
	} = new();

	/// <summary>
	/// 新しいタブを追加する
	/// </summary>
	public void AddTab() {
		var scope = this._rootServiceProvider.CreateScope();
		var tabState = scope.ServiceProvider.GetRequiredService<TabStateModel>();
		for (var num = 1; true; num++) {
			var tabName = $"Tab {num}";
			if (this._rootState.Tabs.All(t => t.DisplayName.Value != tabName)) {
				tabState.DisplayName.Value = tabName;
				break;
			}
		}

		// 通知コンテキストの初期化（所属するWindowを動的に検索する）
		var notifContext = scope.ServiceProvider.GetRequiredService<NotificationContextProvider>();
		var stateStore = scope.ServiceProvider.GetRequiredService<IStateStore>();
		notifContext.TargetWindowIdResolver = () => {
			return stateStore.RootState.Windows.FirstOrDefault(w => w.TabIds.Contains(tabState.TabId))?.WindowId;
		};

		// AppStateのデフォルトタブ状態を新規タブに適用
		var defaultSearch = stateStore.RootState.AppState.DefaultTabState.SearchState;
		var defaultViewer = stateStore.RootState.AppState.DefaultTabState.ViewerState;
		tabState.SearchState.CurrentSortCondition.Value = defaultSearch.CurrentSortCondition.Value;
		tabState.SearchState.SortDirection.Value = defaultSearch.SortDirection.Value;
		tabState.SearchState.CurrentFilteringConditions.Value = [.. defaultSearch.CurrentFilteringConditions.Value];
		tabState.ViewerState.ItemSize.Value = defaultViewer.ItemSize.Value;
		tabState.ViewerState.ShowOverlay.Value = defaultViewer.ShowOverlay.Value;
		tabState.ViewerState.ShowInfo.Value = defaultViewer.ShowInfo.Value;

		// ListViewer 列設定
		tabState.ViewerState.ListFileNameColumnWidth.Value = defaultViewer.ListFileNameColumnWidth.Value;
		tabState.ViewerState.ListResolutionColumnWidth.Value = defaultViewer.ListResolutionColumnWidth.Value;
		tabState.ViewerState.ListFileSizeColumnWidth.Value = defaultViewer.ListFileSizeColumnWidth.Value;
		tabState.ViewerState.ListCreationTimeColumnWidth.Value = defaultViewer.ListCreationTimeColumnWidth.Value;
		tabState.ViewerState.ListRateColumnWidth.Value = defaultViewer.ListRateColumnWidth.Value;
		tabState.ViewerState.ListFileNameColumnVisible.Value = defaultViewer.ListFileNameColumnVisible.Value;
		tabState.ViewerState.ListResolutionColumnVisible.Value = defaultViewer.ListResolutionColumnVisible.Value;
		tabState.ViewerState.ListFileSizeColumnVisible.Value = defaultViewer.ListFileSizeColumnVisible.Value;
		tabState.ViewerState.ListCreationTimeColumnVisible.Value = defaultViewer.ListCreationTimeColumnVisible.Value;
		tabState.ViewerState.ListRateColumnVisible.Value = defaultViewer.ListRateColumnVisible.Value;


		// ルートの状態リストに追加
		this._rootState.Tabs.Add(tabState);
		// 自身のウィンドウのTabIdリストに追加
		this._windowState.TabIds.Add(tabState.TabId);

		// CreateView は同期的であるため、すぐに追加後のタブを取得できる
		var createdTabContext = this.Tabs.FirstOrDefault(x => x.TabState.TabId == tabState.TabId);
		if (createdTabContext != null) {
			this.SelectedTab.Value = createdTabContext;
		}
	}

	/// <summary>
	/// 指定タブを閉じる
	/// </summary>
	public void CloseTab(TabContext tab) {
		tab.Dispose();
		this._windowState.TabIds.Remove(tab.TabState.TabId);
		this._rootState.Tabs.Remove(tab.TabState);

		if (this.SelectedTab.Value == tab) {
			this.SelectedTab.Value = this.Tabs.LastOrDefault();
		}

		// 最後のタブを閉じたら新しいタブを作る
		if (!this.Tabs.Any()) {
			this.AddTab();
		}
	}

	protected override void Dispose(bool disposing) {
		if (disposing) {
			foreach (var tab in this.Tabs) {
				tab.Dispose();
			}
			this.Tabs.Dispose();
		}
		base.Dispose(disposing);
	}
}