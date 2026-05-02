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


	public MainWindowViewModel(
		IServiceProvider serviceProvider,
		IStateStore stateStore,
		WindowStateProvider windowStateProvider,
		BackgroundTasksViewModel backgroundTasksViewModel,
		NavigationMenuViewModel navigationMenuViewModel) {
		this._rootServiceProvider = serviceProvider;
		this.NavigationMenuViewModel = navigationMenuViewModel;

		// 自身のウィンドウの状態を取得（WindowManagerによってセット済み）
		this._windowState = windowStateProvider.State ?? throw new InvalidOperationException("WindowStateModel was not provided to the scope.");

		this.Tabs = this._windowState.Tabs.ToWritableNotifyCollectionChanged(
			tabState => new TabContext(tabState),
			(TabContext tabContext, TabStateModel tabState, ref bool setValue) => {
				setValue = true;
				return tabContext.TabState;
			},
			SynchronizationContextCollectionEventDispatcher.Current);

		this.SelectedTab = this._windowState.SelectedTab.ToTwoWayBindableReactiveProperty(
			x => this.Tabs.FirstOrDefault(t => t.TabState == x),
			x => x?.TabState,
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

		// 自身のウィンドウの状態リストに追加
		this._windowState.Tabs.Add(tabState);

		// CreateView は同期的であるため、すぐに追加後のタブを取得できる
		var createdTabContext = this.Tabs.FirstOrDefault(x => x.TabState == tabState);
		if (createdTabContext != null) {
			this.SelectedTab.Value = createdTabContext;
		}
	}

	/// <summary>
	/// 指定タブを閉じる
	/// </summary>
	public void CloseTab(TabContext tab) {
		tab.Dispose();
		this._windowState.Tabs.Remove(tab.TabState);

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