using MediaDeck.Common.Base;
using MediaDeck.Common.Extensions;
using MediaDeck.Composition.Objects;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Core.Stores.State;
using MediaDeck.ViewModels.Controls;
using MediaDeck.ViewModels.Tabs;

namespace MediaDeck.ViewModels;

[Inject(InjectServiceLifetime.Scoped)]
public class MainWindowViewModel : ViewModelBase {
	private readonly IServiceProvider _rootServiceProvider;
	private readonly ITabScopeFactory _tabScopeFactory;
	private readonly WindowStateModel _windowState;
	private readonly RootStateModel _rootState;


	public Guid WindowId {
		get {
			return this._windowState.WindowId;
		}
	}

	public MainWindowViewModel(
		IServiceProvider serviceProvider,
		IStateStore stateStore,
		WindowStateProvider windowStateProvider,
		ITabScopeFactory tabScopeFactory,
		NavigationMenuViewModel navigationMenuViewModel,
		GlobalInfoBarViewModel globalInfoBarViewModel) {
		this._rootServiceProvider = serviceProvider;
		this._tabScopeFactory = tabScopeFactory;
		this.NavigationMenuViewModel = navigationMenuViewModel;
		this.GlobalInfoBarViewModel = globalInfoBarViewModel.AddTo(this.CompositeDisposable);
		this._rootState = stateStore.RootState;

		// 自身のウィンドウの状態を取得（WindowManagerによってセット済み）
		this._windowState = windowStateProvider.State ?? throw new InvalidOperationException("WindowStateModel was not provided to the scope.");

		this.Tabs = this._windowState.TabIds.ToWritableNotifyCollectionChanged(
				tabId => {
					var tabState = this._rootState.Tabs.FirstOrDefault(t => t.TabId == tabId)
						?? throw new InvalidOperationException($"TabStateModel not found for TabId: {tabId}");
					return this._tabScopeFactory.GetOrCreateTabViewModel(tabState);
				},
			(TabViewModel tabViewModel, Guid tabId, ref bool setValue) => {
				setValue = true;
				return tabViewModel.TabState.TabId;
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
	public INotifyCollectionChangedSynchronizedViewList<TabViewModel> Tabs {
		get;
	}

	/// <summary>
	/// 選択中のタブ
	/// </summary>
	public BindableReactiveProperty<TabViewModel?> SelectedTab {
		get;
	}

	public NavigationMenuViewModel NavigationMenuViewModel {
		get;
	}

	/// <summary>
	/// グローバル情報バー ViewModel
	/// </summary>
	public GlobalInfoBarViewModel GlobalInfoBarViewModel {
		get;
	}

	/// <summary>
	/// ウィンドウ活性化コマンド
	/// </summary>
	public ReactiveCommand WindowActivatedCommand {
		get;
	} = new();

	/// <summary>
	/// タブ追加コマンド
	/// </summary>
	public ReactiveCommand AddTabCommand {
		get;
	} = new();

	/// <summary>
	/// タブ終了コマンド
	/// </summary>
	public ReactiveCommand<TabViewModel> CloseTabCommand {
		get;
	} = new();

	/// <summary>
	/// 新しいタブを追加する
	/// </summary>
	public void AddTab() {
		var tabState = this._tabScopeFactory.CreateTabState();

		// 自身のウィンドウのTabIdリストに追加
		this._windowState.TabIds.Add(tabState.TabId);

		// CreateView は同期的であるため、すぐに追加後のタブを取得できる
		var createdTabContext = this.Tabs.FirstOrDefault(x => x.TabState.TabId == tabState.TabId);
		if (createdTabContext != null) {
			this.SelectedTab.Value = createdTabContext;
		}
	}

	/// <summary>
	/// 指定タブを元ウィンドウのTabIdsから削除する（他ウィンドウへ移動用）。
	/// TabStateModelはRootStateModel.Tabsに残したまま、UI上のTabViewModelのみ切り離す。
	/// 移動先ウィンドウで復元した際にDisposedExceptionが出るのを防ぐため、ここではDisposeしない。
	/// </summary>
	/// <param name="tab">削除するタブViewModel</param>
	public void DetachTab(TabViewModel tab) {
		this._windowState.TabIds.Remove(tab.TabState.TabId);

		if (this.SelectedTab.Value == tab) {
			this.SelectedTab.Value = this.Tabs.LastOrDefault();
		}

		// 最後のタブを移動した際、他にウィンドウがあればこのウィンドウを閉じる
		if (!this.Tabs.Any() && this._rootState.Windows.Count > 1) {
			this._rootState.Windows.Remove(this._windowState);
		}
	}

	/// <summary>
	/// 指定タブを閉じる
	/// </summary>
	public void CloseTab(TabViewModel tab) {
		this._windowState.TabIds.Remove(tab.TabState.TabId);
		this._rootState.Tabs.Remove(tab.TabState);

		if (this.SelectedTab.Value == tab) {
			this.SelectedTab.Value = this.Tabs.LastOrDefault();
		}

		// 最後のタブを閉じたらウィンドウを閉じる。他にウィンドウがあればこのウィンドウを閉じる
		if (!this.Tabs.Any() && this._rootState.Windows.Count > 1) {
			this._rootState.Windows.Remove(this._windowState);
		}
	}

	protected override void Dispose(bool disposing) {
		if (disposing) {
			foreach (var tab in this.Tabs) {
				this._rootState.Tabs.Remove(tab.TabState);
			}
			this.Tabs.Dispose();
		}
		base.Dispose(disposing);
	}

}