using MediaDeck.Common.Base;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Core.Stores.State;
using MediaDeck.ViewModels.Tools;

using Microsoft.Extensions.DependencyInjection;

namespace MediaDeck.ViewModels;

[Inject(InjectServiceLifetime.Singleton)]
public class MainWindowViewModel : ViewModelBase {
	private readonly IServiceProvider _rootServiceProvider;
	private readonly IStateStore _stateStore;


	public MainWindowViewModel(
		IServiceProvider serviceProvider,
		IStateStore stateStore,
		BackgroundTasksViewModel backgroundTasksViewModel,
		NavigationMenuViewModel navigationMenuViewModel) {
		this._rootServiceProvider = serviceProvider;
		this._stateStore = stateStore;
		this.NavigationMenuViewModel = navigationMenuViewModel;

		this.Tabs = stateStore.RootState.Tabs.ToWritableNotifyCollectionChanged(
			tabState => new TabContext(tabState),
			(TabContext tabContext, TabStateModel tabState, ref bool setValue) => {
				setValue = true;
				return tabContext.TabState;
			},
			SynchronizationContextCollectionEventDispatcher.Current);

		this.WindowActivatedCommand.Subscribe(_ => {
			backgroundTasksViewModel.Start();

			// 初回起動時に保存済みタブがあればそれを選択、なければ新規タブ
			if (!this.Tabs.Any()) {
				this.AddTab();
			} else {
				var idx = Math.Clamp(stateStore.RootState.ActiveTabIndex, 0, ((IReadOnlyList<TabContext>)this.Tabs).Count - 1);
				this.SelectedTab.Value = this.Tabs.ElementAt(idx);
			}
		}).AddTo(this.CompositeDisposable);

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
	} = new();

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

		this._stateStore.RegisterTab(tabState);

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
		this._stateStore.UnregisterTab(tab.TabState);

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
			// アクティブタブインデックスを保存
			var selected = this.SelectedTab.Value;
			this._stateStore.RootState.ActiveTabIndex = selected is not null
				? this.Tabs.IndexOf(selected)
				: 0;

			foreach (var tab in this.Tabs) {
				tab.Dispose();
			}
			this.Tabs.Dispose();
		}
		base.Dispose(disposing);
	}
}