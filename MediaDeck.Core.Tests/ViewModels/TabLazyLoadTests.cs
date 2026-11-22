using System.Collections.Concurrent;
using MediaDeck.Common.Base;
using MediaDeck.Composition.Database;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Composition.Interfaces.Primitives;
using MediaDeck.Composition.Interfaces.Services;
using MediaDeck.Composition.Interfaces.Threading;
using MediaDeck.Composition.Objects;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Core.Stores.State;
using MediaDeck.MediaItemTypes.Pdf.Models;
using MediaDeck.ViewModels;
using MediaDeck.ViewModels.Panes.DetailPanes;
using MediaDeck.ViewModels.Panes.FilterPanes;
using MediaDeck.ViewModels.Panes.RepositoryPanes;
using MediaDeck.ViewModels.Panes.SortPanes;
using MediaDeck.ViewModels.Panes.StatusPanes;
using MediaDeck.ViewModels.Panes.ViewerPanes;
using MediaDeck.ViewModels.Tabs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using ObservableCollections;
using R3;
using Shouldly;

namespace MediaDeck.Core.Tests.ViewModels;

/// <summary>
/// 起動時およびウィンドウ・タブ操作時における非アクティブタブ遅延ロードの動作を検証する単体テスト。
/// </summary>
public sealed class TabLazyLoadTests : IDisposable {
	private readonly SynchronizationContext? _previousSyncContext;
	private readonly InlineSynchronizationContext _syncContext;

	/// <summary>
	/// <see cref="TabLazyLoadTests"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	public TabLazyLoadTests() {
		this._previousSyncContext = SynchronizationContext.Current;
		this._syncContext = new InlineSynchronizationContext();
		SynchronizationContext.SetSynchronizationContext(this._syncContext);
	}

	/// <summary>
	/// テスト終了時に同期コンテキストを元の状態へ復元します。
	/// </summary>
	public void Dispose() {
		SynchronizationContext.SetSynchronizationContext(this._previousSyncContext);
	}

	/// <summary>
	/// 複数タブが保存されている状態で起動した際、アクティブタブのみがロードされ、非アクティブタブは未ロードのまま維持されることを検証します。
	/// </summary>
	[Fact]
	public void Startup_LoadsOnlyActiveTab_AndKeepsInactiveTabsUnloaded() {
		using var env = new TestEnvironment();
		var tabState1 = env.CreateTabState("Tab 1");
		var tabState2 = env.CreateTabState("Tab 2");
		var tabState3 = env.CreateTabState("Tab 3");
		var windowState = env.CreateWindowState([tabState1, tabState2, tabState3], tabState2.TabId);

		using var vm = env.CreateMainWindowViewModel(windowState);

		vm.SelectedTab.Value.ShouldNotBeNull();
		vm.SelectedTab.Value.TabState.TabId.ShouldBe(tabState2.TabId);

		var tab1 = vm.Tabs.First(t => t.TabState.TabId == tabState1.TabId);
		var tab2 = vm.Tabs.First(t => t.TabState.TabId == tabState2.TabId);
		var tab3 = vm.Tabs.First(t => t.TabState.TabId == tabState3.TabId);

		tab2.IsLoaded.ShouldBeTrue();
		tab2.Content.Value.ShouldNotBeNull();
		env.GetTracker(tabState2.TabId).FactoryCallCount.ShouldBe(1);
		env.GetTracker(tabState2.TabId).ReloadCallCount.ShouldBe(1);

		tab1.IsLoaded.ShouldBeFalse();
		tab1.Content.Value.ShouldBeNull();
		env.GetTracker(tabState1.TabId).FactoryCallCount.ShouldBe(0);
		env.GetTracker(tabState1.TabId).ReloadCallCount.ShouldBe(0);

		tab3.IsLoaded.ShouldBeFalse();
		tab3.Content.Value.ShouldBeNull();
		env.GetTracker(tabState3.TabId).FactoryCallCount.ShouldBe(0);
		env.GetTracker(tabState3.TabId).ReloadCallCount.ShouldBe(0);
	}

	/// <summary>
	/// 非アクティブタブを初めて選択したタイミングで、そのタブのみコンテンツ生成と初回ロードが実行されることを検証します。
	/// </summary>
	[Fact]
	public void SelectingInactiveTab_LoadsOnlySelectedTabOnFirstActivation() {
		using var env = new TestEnvironment();
		var tabState1 = env.CreateTabState("Tab 1");
		var tabState2 = env.CreateTabState("Tab 2");
		var tabState3 = env.CreateTabState("Tab 3");
		var windowState = env.CreateWindowState([tabState1, tabState2, tabState3], tabState2.TabId);

		using var vm = env.CreateMainWindowViewModel(windowState);
		var tab1 = vm.Tabs.First(t => t.TabState.TabId == tabState1.TabId);
		var tab2 = vm.Tabs.First(t => t.TabState.TabId == tabState2.TabId);
		var tab3 = vm.Tabs.First(t => t.TabState.TabId == tabState3.TabId);

		vm.SelectedTab.Value = tab1;

		tab1.IsLoaded.ShouldBeTrue();
		tab1.Content.Value.ShouldNotBeNull();
		env.GetTracker(tabState1.TabId).FactoryCallCount.ShouldBe(1);
		env.GetTracker(tabState1.TabId).ReloadCallCount.ShouldBe(1);

		env.GetTracker(tabState2.TabId).FactoryCallCount.ShouldBe(1);
		env.GetTracker(tabState2.TabId).ReloadCallCount.ShouldBe(1);

		tab3.IsLoaded.ShouldBeFalse();
		tab3.Content.Value.ShouldBeNull();
		env.GetTracker(tabState3.TabId).FactoryCallCount.ShouldBe(0);
		env.GetTracker(tabState3.TabId).ReloadCallCount.ShouldBe(0);
	}

	/// <summary>
	/// 既にロード済みのタブを再選択しても、コンテンツの再生成および再ロードが重複実行されないことを検証します。
	/// </summary>
	[Fact]
	public void ReselectingAlreadyLoadedTab_IsIdempotent() {
		using var env = new TestEnvironment();
		var tabState1 = env.CreateTabState("Tab 1");
		var tabState2 = env.CreateTabState("Tab 2");
		var windowState = env.CreateWindowState([tabState1, tabState2], tabState2.TabId);

		using var vm = env.CreateMainWindowViewModel(windowState);
		var tab1 = vm.Tabs.First(t => t.TabState.TabId == tabState1.TabId);
		var tab2 = vm.Tabs.First(t => t.TabState.TabId == tabState2.TabId);

		vm.SelectedTab.Value = tab1;
		vm.SelectedTab.Value = tab2;
		tab2.EnsureLoaded();

		env.GetTracker(tabState2.TabId).FactoryCallCount.ShouldBe(1);
		env.GetTracker(tabState2.TabId).ReloadCallCount.ShouldBe(1);
		env.GetTracker(tabState1.TabId).FactoryCallCount.ShouldBe(1);
		env.GetTracker(tabState1.TabId).ReloadCallCount.ShouldBe(1);
	}

	/// <summary>
	/// SelectedTabId が未設定または存在しないGUIDの場合、先頭タブへフォールバックして先頭タブのみロードされることを検証します。
	/// </summary>
	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void Startup_FallsBackToFirstTab_WhenSelectedTabIdIsNullOrInvalid(bool useNullSelectedTabId) {
		using var env = new TestEnvironment();
		var tabState1 = env.CreateTabState("Tab 1");
		var tabState2 = env.CreateTabState("Tab 2");
		var tabState3 = env.CreateTabState("Tab 3");
		var initialSelectedId = useNullSelectedTabId ? (Guid?)null : Guid.NewGuid();
		var windowState = env.CreateWindowState([tabState1, tabState2, tabState3], initialSelectedId);

		using var vm = env.CreateMainWindowViewModel(windowState);

		vm.SelectedTab.Value.ShouldNotBeNull();
		vm.SelectedTab.Value.TabState.TabId.ShouldBe(tabState1.TabId);
		windowState.SelectedTabId.Value.ShouldBe(tabState1.TabId);

		env.GetTracker(tabState1.TabId).FactoryCallCount.ShouldBe(1);
		env.GetTracker(tabState1.TabId).ReloadCallCount.ShouldBe(1);
		env.GetTracker(tabState2.TabId).FactoryCallCount.ShouldBe(0);
		env.GetTracker(tabState2.TabId).ReloadCallCount.ShouldBe(0);
		env.GetTracker(tabState3.TabId).FactoryCallCount.ShouldBe(0);
		env.GetTracker(tabState3.TabId).ReloadCallCount.ShouldBe(0);
	}

	/// <summary>
	/// 新規タブを追加した際、そのタブが選択されて即座にロードされることを検証します。
	/// </summary>
	[Fact]
	public void AddTab_SelectsAndLoadsNewTabImmediately() {
		using var env = new TestEnvironment();
		var tabState1 = env.CreateTabState("Tab 1");
		var windowState = env.CreateWindowState([tabState1], tabState1.TabId);

		using var vm = env.CreateMainWindowViewModel(windowState);
		vm.AddTab();

		var addedTab = vm.SelectedTab.Value;
		addedTab.ShouldNotBeNull();
		addedTab.TabState.TabId.ShouldNotBe(tabState1.TabId);
		addedTab.IsLoaded.ShouldBeTrue();
		addedTab.Content.Value.ShouldNotBeNull();

		var tracker = env.GetTracker(addedTab.TabState.TabId);
		tracker.FactoryCallCount.ShouldBe(1);
		tracker.ReloadCallCount.ShouldBe(1);
	}

	/// <summary>
	/// 未ロードの非アクティブタブを閉じた際、ロードが一度も発生せず例外なく破棄されることを検証します。
	/// </summary>
	[Fact]
	public void CloseTab_UnloadedInactiveTab_DisposesWithoutLoading() {
		using var env = new TestEnvironment();
		var tabState1 = env.CreateTabState("Tab 1");
		var tabState2 = env.CreateTabState("Tab 2");
		var tabState3 = env.CreateTabState("Tab 3");
		var windowState = env.CreateWindowState([tabState1, tabState2, tabState3], tabState1.TabId);

		using var vm = env.CreateMainWindowViewModel(windowState);
		var tab2 = vm.Tabs.First(t => t.TabState.TabId == tabState2.TabId);
		var tab3 = vm.Tabs.First(t => t.TabState.TabId == tabState3.TabId);

		Should.NotThrow(() => vm.CloseTab(tab2));

		env.GetTracker(tabState2.TabId).FactoryCallCount.ShouldBe(0);
		env.GetTracker(tabState2.TabId).ReloadCallCount.ShouldBe(0);
		tab2.DisposeState.ShouldBe(DisposeState.Disposed);
		Should.Throw<ObjectDisposedException>(() => tabState2.ServiceProvider.GetRequiredService<TabStateModel>());

		tab3.IsLoaded.ShouldBeFalse();
		env.GetTracker(tabState3.TabId).FactoryCallCount.ShouldBe(0);
		vm.SelectedTab.Value?.TabState.TabId.ShouldBe(tabState1.TabId);
	}

	/// <summary>
	/// 選択中のタブを閉じた際、次に選択される未ロードタブが自動的にロードされることを検証します。
	/// </summary>
	[Fact]
	public void CloseTab_SelectedTab_AutomaticallyLoadsNextSelectedTab() {
		using var env = new TestEnvironment();
		var tabState1 = env.CreateTabState("Tab 1");
		var tabState2 = env.CreateTabState("Tab 2");
		var tabState3 = env.CreateTabState("Tab 3");
		var windowState = env.CreateWindowState([tabState1, tabState2, tabState3], tabState2.TabId);

		using var vm = env.CreateMainWindowViewModel(windowState);
		var tab1 = vm.Tabs.First(t => t.TabState.TabId == tabState1.TabId);
		var tab2 = vm.Tabs.First(t => t.TabState.TabId == tabState2.TabId);
		var tab3 = vm.Tabs.First(t => t.TabState.TabId == tabState3.TabId);

		vm.CloseTab(tab2);

		vm.SelectedTab.Value.ShouldBeSameAs(tab3);
		tab3.IsLoaded.ShouldBeTrue();
		env.GetTracker(tabState3.TabId).FactoryCallCount.ShouldBe(1);
		env.GetTracker(tabState3.TabId).ReloadCallCount.ShouldBe(1);

		tab1.IsLoaded.ShouldBeFalse();
		env.GetTracker(tabState1.TabId).FactoryCallCount.ShouldBe(0);
	}

	/// <summary>
	/// 未ロードタブを別ウィンドウへ移動しても移動時点ではロードされず、移動先ウィンドウで選択された際に初めてロードされることを検証します。
	/// </summary>
	[Fact]
	public void MoveUnloadedTabToAnotherWindow_DelaysLoadUntilSelectedInTargetWindow() {
		using var env = new TestEnvironment();
		var tabState1 = env.CreateTabState("Tab 1");
		var tabState2 = env.CreateTabState("Tab 2");
		var tabState3 = env.CreateTabState("Tab 3");
		var windowState1 = env.CreateWindowState([tabState1, tabState2], tabState1.TabId);
		var windowState2 = env.CreateWindowState([tabState3], tabState3.TabId);

		using var vm1 = env.CreateMainWindowViewModel(windowState1);
		using var vm2 = env.CreateMainWindowViewModel(windowState2);

		var tab2InWindow1 = vm1.Tabs.First(t => t.TabState.TabId == tabState2.TabId);
		vm1.DetachTab(tab2InWindow1);
		windowState2.TabIds.Add(tabState2.TabId);

		var tab2InWindow2 = vm2.Tabs.First(t => t.TabState.TabId == tabState2.TabId);
		tab2InWindow2.ShouldBeSameAs(tab2InWindow1);
		tab2InWindow2.DisposeState.ShouldBe(DisposeState.NotDisposed);
		tab2InWindow2.IsLoaded.ShouldBeFalse();
		env.GetTracker(tabState2.TabId).FactoryCallCount.ShouldBe(0);
		env.GetTracker(tabState2.TabId).ReloadCallCount.ShouldBe(0);

		vm2.SelectedTab.Value = tab2InWindow2;

		tab2InWindow2.IsLoaded.ShouldBeTrue();
		env.GetTracker(tabState2.TabId).FactoryCallCount.ShouldBe(1);
		env.GetTracker(tabState2.TabId).ReloadCallCount.ShouldBe(1);
	}

	/// <summary>
	/// 破棄済みの <see cref="TabViewModel"/> に対して <see cref="TabViewModel.EnsureLoaded"/> を呼んでも例外が発生せずロードも実行されないことを検証します。
	/// </summary>
	[Fact]
	public void EnsureLoaded_WhenDisposed_DoesNotThrowOrLoad() {
		using var env = new TestEnvironment();
		var tabState = env.CreateTabState("Disposed Tab");
		var tabViewModel = env.TabScopeFactory.GetOrCreateTabViewModel(tabState);

		tabViewModel.Dispose();
		Should.NotThrow(() => tabViewModel.EnsureLoaded());

		tabViewModel.IsLoaded.ShouldBeFalse();
		tabViewModel.Content.Value.ShouldBeNull();
		env.GetTracker(tabState.TabId).FactoryCallCount.ShouldBe(0);
		env.GetTracker(tabState.TabId).ReloadCallCount.ShouldBe(0);
	}

	/// <summary>
	/// 未ロード状態でも表示名が同期されること、およびロード前後で <see cref="AppStateModel.DefaultTabState"/> が意図せず上書きされずロード後の変更のみ反映されることを検証します。
	/// </summary>
	[Fact]
	public void DisplayNameAndDefaultTabStateSync_BehaveCorrectlyBeforeAndAfterLazyLoad() {
		using var env = new TestEnvironment();
		var defaultTabState = env.StateStore.RootState.AppState.DefaultTabState;
		defaultTabState.LeftPaneWidth.Value = 300;
		defaultTabState.ActiveRepository.Value = RepositoryType.Album;

		var tabState1 = env.CreateTabState("Tab 1");
		var tabState2 = env.CreateTabState("Tab 2", state => {
			state.LeftPaneWidth.Value = 180;
			state.ActiveRepository.Value = RepositoryType.Folder;
		});
		var windowState = env.CreateWindowState([tabState1, tabState2], tabState1.TabId);

		using var vm = env.CreateMainWindowViewModel(windowState);
		var tab2 = vm.Tabs.First(t => t.TabState.TabId == tabState2.TabId);

		// 未ロード状態でも DisplayName は同期される
		tabState2.DisplayName.Value = "Renamed Lazy Tab";
		tab2.DisplayName.Value.ShouldBe("Renamed Lazy Tab");

		// 未ロード状態での変更は DefaultTabState へ反映されない
		tabState2.LeftPaneWidth.Value = 210;
		tabState2.ActiveRepository.Value = RepositoryType.Album;
		tabState2.ActiveRepository.Value = RepositoryType.Folder;
		defaultTabState.LeftPaneWidth.Value.ShouldBe(300);
		defaultTabState.ActiveRepository.Value.ShouldBe(RepositoryType.Album);

		// ロード直後も初期値で DefaultTabState を上書きしない（Skip(1) による保護）
		vm.SelectedTab.Value = tab2;
		tab2.IsLoaded.ShouldBeTrue();
		defaultTabState.LeftPaneWidth.Value.ShouldBe(300);
		defaultTabState.ActiveRepository.Value.ShouldBe(RepositoryType.Album);

		// ロード後の変更は DefaultTabState へ同期される
		tabState2.LeftPaneWidth.Value = 260;
		tabState2.ActiveRepository.Value = RepositoryType.Album;
		tabState2.ActiveRepository.Value = RepositoryType.Folder;
		defaultTabState.LeftPaneWidth.Value.ShouldBe(260);
		defaultTabState.ActiveRepository.Value.ShouldBe(RepositoryType.Folder);
	}

	/// <summary>
	/// 実 DI コンテナ上の <see cref="TabScopeFactory"/> において、<see cref="TabScopeFactory.GetOrCreateTabViewModel"/> 時点では
	/// <see cref="TabContentViewModel"/> が解決されず、<see cref="TabViewModel.EnsureLoaded"/> 時に解決・初期化され、
	/// タブ削除時にロード済み・未ロードを問わずスコープが破棄されることを検証します。
	/// </summary>
	[Fact]
	public void TabScopeFactory_Integration_DelaysContentResolutionUntilEnsureLoadedAndDisposesScopesOnRemove() {
		using var dbFactory = new SqliteTestDbFactory();
		var pathProvider = new StubAppPathProvider();
		try {
			var contentResolvedCount = 0;
			var services = new ServiceCollection();
			ConfigureCommonServices(services, dbFactory, pathProvider);

			// 実 DI コンテナの依存解決を使いつつ、TabContentViewModel の解決回数を計測できるようにラップする
			services.AddScoped<TabContentViewModel>(sp => {
				Interlocked.Increment(ref contentResolvedCount);
				return ActivatorUtilities.CreateInstance<TabContentViewModel>(sp);
			});

			using var rootProvider = services.BuildServiceProvider();
			var stateStore = rootProvider.GetRequiredService<IStateStore>();
			var tabScopeFactory = rootProvider.GetRequiredService<ITabScopeFactory>();

			var tabState1 = tabScopeFactory.CreateTabState("Integration Tab 1");
			var tabState2 = tabScopeFactory.CreateTabState("Integration Tab 2");

			var windowState = rootProvider.GetRequiredService<WindowStateModel>();
			windowState.TabIds.Add(tabState1.TabId);
			windowState.TabIds.Add(tabState2.TabId);
			stateStore.RootState.Windows.Add(windowState);

			var tab1 = tabScopeFactory.GetOrCreateTabViewModel(tabState1);
			var tab2 = tabScopeFactory.GetOrCreateTabViewModel(tabState2);

			// GetOrCreateTabViewModel 時点ではまだ TabContentViewModel も通知コンテキストも解決・設定されていない
			contentResolvedCount.ShouldBe(0);
			tab1.IsLoaded.ShouldBeFalse();
			tab2.IsLoaded.ShouldBeFalse();
			tabState1.ServiceProvider.GetRequiredService<NotificationContextProvider>().TargetWindowIdResolver.ShouldBeNull();
			tabState2.ServiceProvider.GetRequiredService<NotificationContextProvider>().TargetWindowIdResolver.ShouldBeNull();

			// tab1 のみ EnsureLoaded を実行
			tab1.EnsureLoaded();

			contentResolvedCount.ShouldBe(1);
			tab1.IsLoaded.ShouldBeTrue();
			tab1.Content.Value.ShouldNotBeNull();
			tab2.IsLoaded.ShouldBeFalse();
			tab2.Content.Value.ShouldBeNull();

			var resolver1 = tabState1.ServiceProvider.GetRequiredService<NotificationContextProvider>().TargetWindowIdResolver;
			resolver1.ShouldNotBeNull();
			resolver1().ShouldBe(windowState.WindowId);
			tabState2.ServiceProvider.GetRequiredService<NotificationContextProvider>().TargetWindowIdResolver.ShouldBeNull();

			var content1 = tab1.Content.Value;

			// ロード済みタブ・未ロードタブの双方を RootState.Tabs から削除し、どちらもスコープごと破棄されることを確認
			stateStore.RootState.Tabs.Remove(tabState1);
			stateStore.RootState.Tabs.Remove(tabState2);

			tab1.DisposeState.ShouldBe(DisposeState.Disposed);
			content1.DisposeState.ShouldBe(DisposeState.Disposed);
			tab2.DisposeState.ShouldBe(DisposeState.Disposed);

			Should.Throw<ObjectDisposedException>(() => tabState1.ServiceProvider.GetRequiredService<TabStateModel>());
			Should.Throw<ObjectDisposedException>(() => tabState2.ServiceProvider.GetRequiredService<TabStateModel>());
		} finally {
			pathProvider.Cleanup();
		}
	}

	/// <summary>
	/// 実 DI コンテナで構築された <see cref="ViewerSelectorViewModel"/> および <see cref="RepositorySelectorViewModel"/> が
	/// <see cref="TabStateModel"/> に応じた正しい種別列挙値を保持し、選択変更時に追従・同期することを検証します。
	/// </summary>
	[Fact]
	public void SelectorViewModels_ExposeMatchingEnumTypesAndSyncWithTabState() {
		using var env = new TestEnvironment();
		var tabState = env.CreateTabState("Selector Tab", state => {
			state.ViewerState.ActiveViewer.Value = ViewerType.Detail;
			state.ActiveRepository.Value = RepositoryType.Album;
		});
		var windowState = env.CreateWindowState([tabState], tabState.TabId);

		using var vm = env.CreateMainWindowViewModel(windowState);
		var tab = vm.SelectedTab.Value;
		tab.ShouldNotBeNull();
		var content = tab.Content.Value;
		content.ShouldNotBeNull();

		// ViewerSelectorViewModel の初期値・各ビューア種別・切り替え追従を確認
		var viewerSelector = content.ViewerSelectorViewModel;
		viewerSelector.WrapViewerViewModel.ViewerType.ShouldBe(ViewerType.Wrap);
		viewerSelector.ListViewerViewModel.ViewerType.ShouldBe(ViewerType.List);
		viewerSelector.DetailViewerViewModel.ViewerType.ShouldBe(ViewerType.Detail);
		viewerSelector.MapViewerViewModel.ViewerType.ShouldBe(ViewerType.Map);

		viewerSelector.SelectedViewerPane.Value.ShouldBeSameAs(viewerSelector.DetailViewerViewModel);
		viewerSelector.SelectedViewerPane.Value.ViewerType.ShouldBe(ViewerType.Detail);

		foreach (var pane in new ViewerPaneViewModelBase[] {
			viewerSelector.WrapViewerViewModel,
			viewerSelector.ListViewerViewModel,
			viewerSelector.DetailViewerViewModel,
			viewerSelector.MapViewerViewModel
		}) {
			viewerSelector.SelectedViewerPane.Value = pane;
			viewerSelector.SelectedViewerPane.Value.ViewerType.ShouldBe(pane.ViewerType);
			tabState.ViewerState.ActiveViewer.Value.ShouldBe(pane.ViewerType);
		}

		// RepositorySelectorViewModel の初期値・各リポジトリ種別・双方向同期を確認
		var repoSelector = content.RepositorySelectorViewModel;
		repoSelector.FolderRepositoryViewModel.RepositoryType.ShouldBe(RepositoryType.Folder);
		repoSelector.AlbumRepositoryViewModel.RepositoryType.ShouldBe(RepositoryType.Album);

		repoSelector.SelectedRepositoryPane.Value.ShouldBeSameAs(repoSelector.AlbumRepositoryViewModel);
		repoSelector.SelectedRepositoryPane.Value.RepositoryType.ShouldBe(RepositoryType.Album);

		repoSelector.SelectedRepositoryPane.Value = repoSelector.FolderRepositoryViewModel;
		repoSelector.SelectedRepositoryPane.Value.RepositoryType.ShouldBe(RepositoryType.Folder);
		tabState.ActiveRepository.Value.ShouldBe(RepositoryType.Folder);

		tabState.ActiveRepository.Value = RepositoryType.Album;
		repoSelector.SelectedRepositoryPane.Value.ShouldBeSameAs(repoSelector.AlbumRepositoryViewModel);
		repoSelector.SelectedRepositoryPane.Value.RepositoryType.ShouldBe(RepositoryType.Album);
	}

	/// <summary>
	/// DI コンテナへテスト用の共通サービス群を登録します。
	/// </summary>
	/// <param name="services">登録先のサービスコレクション。</param>
	/// <param name="dbFactory">テスト用 DB ファクトリ。</param>
	/// <param name="pathProvider">テスト用パスプロバイダー。</param>
	private static void ConfigureCommonServices(
		ServiceCollection services,
		SqliteTestDbFactory dbFactory,
		StubAppPathProvider pathProvider) {
		services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.None));

		global::MediaDeck.Composition.DIRegistration.AddGeneratedServices(services);
		global::MediaDeck.MediaItemTypes.DIRegistration.AddGeneratedServices(services);
		global::MediaDeck.Core.DIRegistration.AddGeneratedServices(services);
		global::MediaDeck.ViewModels.DIRegistration.AddGeneratedServices(services);
		global::MediaDeck.Store.DIRegistration.AddGeneratedServices(services);

		services.AddSingleton<IDbContextFactory<MediaDeckDbContext>>(dbFactory);
		services.AddSingleton<IAppPathProvider>(pathProvider);
		services.AddSingleton<IStringProvider, StubStringProvider>();

		var uiDispatcherMock = new Mock<IUiDispatcher>();
		uiDispatcherMock.Setup(x => x.Run(It.IsAny<Action>())).Callback<Action>(a => a());
		uiDispatcherMock.Setup(x => x.RunAsync(It.IsAny<Action>())).Returns<Action>(a => {
			a();
			return Task.CompletedTask;
		});
		uiDispatcherMock.Setup(x => x.RunAsync(It.IsAny<Func<Task>>())).Returns<Func<Task>>(async a => await a());
		services.AddSingleton(uiDispatcherMock.Object);

		var locationFactoryMock = new Mock<ILocationFactory>();
		locationFactoryMock.Setup(x => x.Create(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double?>()))
			.Returns<double, double, double?>((lat, lon, alt) => {
				var loc = new Mock<ILocation>();
				loc.Setup(l => l.Latitude).Returns(lat);
				loc.Setup(l => l.Longitude).Returns(lon);
				loc.Setup(l => l.Altitude).Returns(alt);
				return loc.Object;
			});
		services.AddSingleton(locationFactoryMock.Object);

		var appEnvMock = new Mock<IAppEnvironmentService>();
		appEnvMock.Setup(x => x.IsUnpackaged).Returns(true);
		services.AddSingleton(appEnvMock.Object);

		var appUpdateMock = new Mock<IAppUpdateService>();
		appUpdateMock.Setup(x => x.AvailableUpdate).Returns(new ReactiveProperty<AppUpdateInfo?>(null));
		services.AddSingleton(appUpdateMock.Object);

		services.AddSingleton(new Mock<IFilePickerService>().Object);
		services.AddTransient(_ => new Mock<IPdfDocumentOperator>().Object);
	}

	/// <summary>
	/// コンストラクタからの直接テスト用 DI 環境・トラッキング用 <see cref="ITabScopeFactory"/> を提供するテストフィクスチャ。
	/// </summary>
	private sealed class TestEnvironment : IDisposable {
		private readonly SqliteTestDbFactory _dbFactory = new();
		private readonly StubAppPathProvider _pathProvider = new();
		private readonly ServiceProvider _rootProvider;
		private readonly List<IServiceScope> _windowScopes = [];

		/// <summary>
		/// <see cref="TestEnvironment"/> クラスの新しいインスタンスを初期化します。
		/// </summary>
		public TestEnvironment() {
			var services = new ServiceCollection();
			ConfigureCommonServices(services, this._dbFactory, this._pathProvider);
			this._rootProvider = services.BuildServiceProvider();
			this.StateStore = this._rootProvider.GetRequiredService<IStateStore>();
			this.TabScopeFactory = new TrackingTabScopeFactory(this._rootProvider, this.StateStore);
		}

		/// <summary>
		/// アプリケーション状態ストアを取得します。
		/// </summary>
		public IStateStore StateStore {
			get;
		}

		/// <summary>
		/// 呼び出し回数を追跡可能なテスト用 <see cref="ITabScopeFactory"/> を取得します。
		/// </summary>
		public TrackingTabScopeFactory TabScopeFactory {
			get;
		}

		/// <summary>
		/// 指定されたタブIDの生成・リロード回数トラッカーを取得します。
		/// </summary>
		/// <param name="tabId">タブID。</param>
		/// <returns>トラッカーインスタンス。</returns>
		public TabLoadTracker GetTracker(Guid tabId) {
			return this.TabScopeFactory.GetTracker(tabId);
		}

		/// <summary>
		/// 新しい <see cref="TabStateModel"/> を生成し、<see cref="RootStateModel.Tabs"/> へ登録します。
		/// </summary>
		/// <param name="displayName">タブ表示名。</param>
		/// <param name="configure">追加設定用デリゲート。</param>
		/// <returns>生成されたタブ状態モデル。</returns>
		public TabStateModel CreateTabState(string displayName, Action<TabStateModel>? configure = null) {
			var tabState = this.TabScopeFactory.CreateTabState(displayName);
			configure?.Invoke(tabState);
			return tabState;
		}

		/// <summary>
		/// 新しい <see cref="WindowStateModel"/> を生成し、<see cref="RootStateModel.Windows"/> へ登録します。
		/// </summary>
		/// <param name="tabs">ウィンドウに属するタブ状態一覧。</param>
		/// <param name="selectedTabId">選択中タブID。</param>
		/// <returns>生成されたウィンドウ状態モデル。</returns>
		public WindowStateModel CreateWindowState(IEnumerable<TabStateModel> tabs, Guid? selectedTabId) {
			var windowState = this._rootProvider.GetRequiredService<WindowStateModel>();
			foreach (var tab in tabs) {
				windowState.TabIds.Add(tab.TabId);
			}
			windowState.SelectedTabId.Value = selectedTabId;
			this.StateStore.RootState.Windows.Add(windowState);
			return windowState;
		}

		/// <summary>
		/// 指定されたウィンドウ状態に対応する <see cref="MainWindowViewModel"/> を構築します。
		/// </summary>
		/// <param name="windowState">ウィンドウ状態モデル。</param>
		/// <returns>構築された <see cref="MainWindowViewModel"/>。</returns>
		public MainWindowViewModel CreateMainWindowViewModel(WindowStateModel windowState) {
			var windowScope = this._rootProvider.CreateScope();
			this._windowScopes.Add(windowScope);

			var provider = windowScope.ServiceProvider.GetRequiredService<WindowStateProvider>();
			provider.State = windowState;

			var notifContext = windowScope.ServiceProvider.GetRequiredService<NotificationContextProvider>();
			notifContext.TargetWindowIdResolver = () => windowState.WindowId;

			return ActivatorUtilities.CreateInstance<MainWindowViewModel>(windowScope.ServiceProvider, this.TabScopeFactory);
		}

		/// <summary>
		/// テストリソースと一時ファイルをすべて解放します。
		/// </summary>
		public void Dispose() {
			this.TabScopeFactory.Dispose();
			foreach (var windowScope in this._windowScopes) {
				windowScope.Dispose();
			}
			this._rootProvider.Dispose();
			this._dbFactory.Dispose();
			this._pathProvider.Cleanup();
		}
	}

	/// <summary>
	/// タブごとの <see cref="TabContentViewModel"/> 生成回数と <see cref="TabContentViewModel.Reload"/> 実行回数を記録するトラッカー。
	/// </summary>
	private sealed class TabLoadTracker {
		/// <summary>
		/// <see cref="TabContentViewModel"/> ファクトリが呼ばれた回数を取得または設定します。
		/// </summary>
		public int FactoryCallCount {
			get;
			set;
		}

		/// <summary>
		/// <see cref="TabContentViewModel.Reload"/> が呼ばれた回数を取得または設定します。
		/// </summary>
		public int ReloadCallCount {
			get;
			set;
		}
	}

	/// <summary>
	/// <see cref="TabContentViewModel.Reload"/> の実行回数を計測し、非同期DBクエリを抑止するテスト用サブクラス。
	/// </summary>
	private sealed class TrackingTabContentViewModel(
		TabStateModel tabState,
		ViewerSelectorViewModel viewerSelectorViewModel,
		FilterSelectorViewModel filterSelectorViewModel,
		SortSelectorViewModel sortSelectorViewModel,
		DetailSelectorViewModel detailSelectorViewModel,
		RepositorySelectorViewModel repositorySelectorViewModel,
		StatusBarViewModel statusBarViewModel,
		TabLoadTracker tracker)
		: TabContentViewModel(
			tabState,
			viewerSelectorViewModel,
			filterSelectorViewModel,
			sortSelectorViewModel,
			detailSelectorViewModel,
			repositorySelectorViewModel,
			statusBarViewModel) {
		/// <inheritdoc />
		public override void Reload() {
			tracker.ReloadCallCount++;
		}
	}

	/// <summary>
	/// タブ生成・初回ロード・破棄の呼び出し回数を決定論的に検証するためのテスト用 <see cref="ITabScopeFactory"/> 実装。
	/// </summary>
	private sealed class TrackingTabScopeFactory : DisposableBase, ITabScopeFactory {
		private readonly IServiceProvider _rootServiceProvider;
		private readonly IStateStore _stateStore;
		private readonly ConcurrentDictionary<Guid, TabViewModel> _tabViewModels = new();
		private readonly ConcurrentDictionary<Guid, TabLoadTracker> _trackers = new();

		/// <summary>
		/// <see cref="TrackingTabScopeFactory"/> クラスの新しいインスタンスを初期化します。
		/// </summary>
		/// <param name="rootServiceProvider">ルートサービスプロバイダー。</param>
		/// <param name="stateStore">アプリケーション状態ストア。</param>
		public TrackingTabScopeFactory(IServiceProvider rootServiceProvider, IStateStore stateStore) {
			this._rootServiceProvider = rootServiceProvider;
			this._stateStore = stateStore;

			this._stateStore.RootState.Tabs.ObserveRemove()
				.Subscribe(x => this.OnTabStateRemoved(x.Value))
				.AddTo(this.CompositeDisposable);
		}

		/// <summary>
		/// 指定されたタブIDのロードトラッカーを取得または作成します。
		/// </summary>
		/// <param name="tabId">タブID。</param>
		/// <returns>ロードトラッカー。</returns>
		public TabLoadTracker GetTracker(Guid tabId) {
			return this._trackers.GetOrAdd(tabId, _ => new TabLoadTracker());
		}

		/// <inheritdoc />
		public TabStateModel CreateTabState(string? displayName = null) {
			var scope = this._rootServiceProvider.CreateScope();
			try {
				var tabState = scope.ServiceProvider.GetRequiredService<TabStateModel>();
				if (string.IsNullOrWhiteSpace(displayName)) {
					for (var num = 1; true; num++) {
						var candidateName = $"Tab {num}";
						if (this._stateStore.RootState.Tabs.All(t => t.DisplayName.Value != candidateName)) {
							tabState.DisplayName.Value = candidateName;
							break;
						}
					}
				} else {
					tabState.DisplayName.Value = displayName;
				}

				tabState.ApplyDefaultState(this._stateStore.RootState.AppState.DefaultTabState);
				this._stateStore.RootState.Tabs.Add(tabState);
				return tabState;
			} catch {
				scope.Dispose();
				throw;
			}
		}

		/// <inheritdoc />
		public TabViewModel GetOrCreateTabViewModel(TabStateModel tabState) {
			if (this._tabViewModels.TryGetValue(tabState.TabId, out var existing)) {
				return existing;
			}

			var tabViewModel = new TabViewModel(tabState, this._stateStore, () => {
				var tracker = this.GetTracker(tabState.TabId);
				tracker.FactoryCallCount++;
				return ActivatorUtilities.CreateInstance<TrackingTabContentViewModel>(tabState.ServiceProvider, tracker);
			});
			this._tabViewModels[tabState.TabId] = tabViewModel;
			return tabViewModel;
		}

		/// <summary>
		/// タブ削除時に対応する <see cref="TabViewModel"/> とタブスコープを破棄します。
		/// </summary>
		/// <param name="tabState">削除されたタブ状態モデル。</param>
		private void OnTabStateRemoved(TabStateModel tabState) {
			if (this._tabViewModels.TryRemove(tabState.TabId, out var viewModel)) {
				viewModel.Dispose();
			}
			(tabState.ServiceProvider as IDisposable)?.Dispose();
		}

		/// <inheritdoc />
		protected override void Dispose(bool disposing) {
			if (disposing) {
				foreach (var kvp in this._tabViewModels) {
					kvp.Value.Dispose();
				}
				this._tabViewModels.Clear();

				foreach (var tabState in this._stateStore.RootState.Tabs) {
					(tabState.ServiceProvider as IDisposable)?.Dispose();
				}
			}
			base.Dispose(disposing);
		}
	}

	/// <summary>
	/// テストスレッド上で同期的にコールバックを実行するインライン同期コンテキスト。
	/// </summary>
	private sealed class InlineSynchronizationContext : SynchronizationContext {
		/// <inheritdoc />
		public override void Post(SendOrPostCallback d, object? state) {
			d(state);
		}

		/// <inheritdoc />
		public override void Send(SendOrPostCallback d, object? state) {
			d(state);
		}
	}
}