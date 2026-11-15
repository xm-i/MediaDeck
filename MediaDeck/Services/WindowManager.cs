using System.Collections.Generic;
using MediaDeck.Common.Base;
using MediaDeck.Composition.Interfaces.Services;
using MediaDeck.Composition.Objects;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Core.Stores.State;
using MediaDeck.ViewModels.Tabs;
using MediaDeck.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using ObservableCollections;
using WinRT.Interop;

namespace MediaDeck.Services;

/// <summary>
/// ウィンドウ単位のコンテキスト実装。
/// ウィンドウ固有のDIスコープのライフサイクルを管理する。
/// </summary>
public class WindowContext(IServiceScope scope, Guid windowId) : IWindowContext {
	private readonly IServiceScope _scope = scope;

	public IServiceProvider Services {
		get {
			return this._scope.ServiceProvider;
		}
	}

	public Guid WindowId {
		get;
	} = windowId;

	internal Window? Window {
		get; private set;
	}

	internal void SetWindow(Window window) {
		this.Window = window;
	}

	public void Dispose() {
		this._scope.Dispose();
	}
}

/// <summary>
/// ウィンドウの生成・管理・終了を一元管理するサービス。
/// </summary>
[Inject(InjectServiceLifetime.Singleton)]
public class WindowManager : DisposableBase {
	private readonly IServiceProvider _rootProvider;
	private readonly IStateStore _stateStore;
	private readonly ITabScopeFactory _tabScopeFactory;
	private readonly ILogger<WindowManager> _logger;
	private readonly List<WindowContext> _windows = [];

	public int WindowCount {
		get {
			return this._windows.Count;
		}
	}

	/// <summary>
	/// 管理されているすべてのウィンドウコンテキストを取得します。
	/// </summary>
	public IReadOnlyList<WindowContext> Windows {
		get {
			return this._windows.AsReadOnly();
		}
	}

	public WindowManager(
		IServiceProvider rootProvider,
		IStateStore stateStore,
		ITabScopeFactory tabScopeFactory,
		ILogger<WindowManager> logger) {
		this._rootProvider = rootProvider;
		this._stateStore = stateStore;
		this._tabScopeFactory = tabScopeFactory;
		this._logger = logger;

		// 状態の変化を監視してウィンドウを開閉する
		this._stateStore.RootState.Windows.ObserveAdd()
			.Subscribe(x => this.OnWindowStateAdded(x.Value))
			.AddTo(this.CompositeDisposable);

		this._stateStore.RootState.Windows.ObserveRemove()
			.Subscribe(x => this.OnWindowStateRemoved(x.Value))
			.AddTo(this.CompositeDisposable);
	}

	/// <summary>
	/// 新しいウィンドウ状態をリストに追加する。
	/// これにより、リアクティブにウィンドウが開かれる。
	/// </summary>
	public void CreateAndShowWindow(AppActivationArguments? args = null, Guid? windowId = null) {
		if (windowId.HasValue && this._stateStore.RootState.Windows.Any(x => x.WindowId == windowId.Value)) {
			// 既に存在する場合は何もしない（またはアクティブにするなどの処理）
			return;
		}

		var newState = this._rootProvider.GetRequiredService<WindowStateModel>();
		newState.WindowId = windowId ?? Guid.NewGuid();
		this._stateStore.RootState.Windows.Add(newState);
	}

	/// <summary>
	/// 保存された状態からすべてのウィンドウを復元する。
	/// </summary>
	public void RestoreWindows() {
		this.CleanAndAdjustState();

		var list = this._stateStore.RootState.Windows.ToList();
		foreach (var windowState in list) {
			this.OnWindowStateAdded(windowState);
		}
	}

	/// <summary>
	/// 起動時のウィンドウとタブの状態をクリーンアップおよび調整します。
	/// </summary>
	private void CleanAndAdjustState() {
		var rootState = this._stateStore.RootState;

		// 1. 所属ウィンドウのないタブをクリーンアップ（RootState.Tabsからの削除によりTabScopeFactoryが自動的にスコープを破棄）
		var activeTabIds = rootState.Windows
			.SelectMany(w => w.TabIds)
			.ToHashSet();

		var orphanedTabs = rootState.Tabs
			.Where(t => !activeTabIds.Contains(t.TabId))
			.ToList();

		foreach (var tab in orphanedTabs) {
			rootState.Tabs.Remove(tab);
			this._logger.LogInformation("所属ウィンドウのないタブを削除しました: {TabId}", tab.TabId);
		}

		// 2. ウィンドウが複数あり、タブ数が0のウィンドウがあれば削除する
		if (rootState.Windows.Count > 1) {
			var emptyWindows = rootState.Windows.Where(w => w.TabIds.Count == 0).ToList();
			foreach (var w in emptyWindows) {
				rootState.Windows.Remove(w);
				this._logger.LogInformation("起動時にタブのないウィンドウを削除しました: {WindowId}", w.WindowId);
			}
		}

		// 3. 保存されたウィンドウ情報がなければ（あるいは上の処理で0件になったら）、空のウィンドウを1つ追加しておく
		if (rootState.Windows.Count == 0) {
			rootState.Windows.Add(this._rootProvider.GetRequiredService<WindowStateModel>());
		}

		// 4. ウィンドウが1つで、かつそのウィンドウのタブ数が0の場合、デフォルト設定でタブを1つ追加する
		if (rootState.Windows.Count == 1 && rootState.Windows[0].TabIds.Count == 0) {
			var windowState = rootState.Windows[0];

			var tabState = this._tabScopeFactory.CreateTabState("Tab 1");

			windowState.TabIds.Add(tabState.TabId);
			windowState.SelectedTabId.Value = tabState.TabId;

			this._logger.LogInformation("起動時にウィンドウが1つでタブが0だったため、新規タブを追加しました: {TabId}", tabState.TabId);
		}
	}

	public void CloseWindow(IWindowContext windowContext) {
		var ws = this._stateStore.RootState.Windows.FirstOrDefault(x => x.WindowId == windowContext.WindowId);
		if (ws != null) {
			this._stateStore.RootState.Windows.Remove(ws);
		}
	}

	/// <summary>
	/// AppWindow.Idから対応するウィンドウのGuidを検索する。
	/// </summary>
	public Guid? FindWindowGuidByAppWindowId(WindowId appWindowId) {
		foreach (var context in this._windows) {
			if (context.Window != null) {
				var windowAppId = GetAppWindowId(context.Window);
				if (windowAppId.Value == appWindowId.Value) {
					return context.WindowId;
				}
			}
		}
		return null;
	}

	/// <inheritdoc />
	public Window? GetWindowFromElement(UIElement element) {
		var xamlRoot = element.XamlRoot;
		if (xamlRoot == null) {
			return null;
		}
		return this._windows.FirstOrDefault(x => x.Window?.Content?.XamlRoot == xamlRoot)?.Window;
	}

	/// <summary>
	/// 指定されたUI要素が所属するウィンドウのGuidを取得する。
	/// </summary>
	/// <param name="element">UI要素</param>
	/// <returns>ウィンドウのGuid。見つからない場合はnull。</returns>
	public Guid? GetWindowIdFromElement(UIElement element) {
		var xamlRoot = element.XamlRoot;
		if (xamlRoot == null) {
			return null;
		}
		return this._windows.FirstOrDefault(x => x.Window?.Content?.XamlRoot == xamlRoot)?.WindowId;
	}

	/// <summary>
	/// WindowオブジェクトからAppWindow.Idを取得するヘルパー。
	/// </summary>
	private static WindowId GetAppWindowId(Window window) {
		var hWnd = WindowNative.GetWindowHandle(window);
		return Win32Interop.GetWindowIdFromWindow(hWnd);
	}

	/// <summary>
	/// 指定されたWindowIdのウィンドウのボーダーをハイライトする。
	/// ホバー時に他ウィンドウを視覚的に識別するために使用される。
	/// </summary>
	/// <param name="windowId">ハイライトするウィンドウID</param>
	public void HighlightWindow(Guid windowId) {
		var windowContext = this._windows.FirstOrDefault(x => x.WindowId == windowId);
		if (windowContext?.Window is MainWindow mainWindow) {
			mainWindow.HighlightBorder(true);
		}
	}

	/// <summary>
	/// 指定されたWindowIdのウィンドウのボーダーハイライトを解除する。
	/// ホバーを外したときに元のボーダー色に戻すために使用される。
	/// </summary>
	/// <param name="windowId">ハイライトを解除するウィンドウID</param>
	public void UnhighlightWindow(Guid windowId) {
		var windowContext = this._windows.FirstOrDefault(x => x.WindowId == windowId);
		if (windowContext?.Window is MainWindow mainWindow) {
			mainWindow.HighlightBorder(false);
		}
	}

	private void OnWindowStateAdded(WindowStateModel windowState) {
		if (this._windows.Any(x => x.WindowId == windowState.WindowId)) {
			return;
		}

		var windowScope = this._rootProvider.CreateScope();
		var windowContext = new WindowContext(windowScope, windowState.WindowId);

		// スコープに状態をセット
		var provider = windowScope.ServiceProvider.GetRequiredService<WindowStateProvider>();
		provider.State = windowState;

		// 通知コンテキストの初期化
		var notifContext = windowScope.ServiceProvider.GetRequiredService<NotificationContextProvider>();
		notifContext.TargetWindowIdResolver = () => windowState.WindowId;

		var window = windowScope.ServiceProvider.GetRequiredService<MainWindow>();
		windowContext.SetWindow(window);

		window.Closed += (_, _) => {
			this.OnWindowUIClosed(windowContext);
		};

		this._windows.Add(windowContext);
		this._logger.LogInformation("ウィンドウを開きました (WindowId={WindowId})", windowState.WindowId);
		window.Activate();
	}

	private void OnWindowStateRemoved(WindowStateModel windowState) {
		var context = this._windows.FirstOrDefault(x => x.WindowId == windowState.WindowId);
		if (context != null) {
			context.Window?.Close();
			this._windows.Remove(context);
			context.Dispose();
			this._logger.LogInformation("ウィンドウを閉じました (WindowId={WindowId})", windowState.WindowId);
		}
	}

	private void OnWindowUIClosed(WindowContext windowContext) {
		// 他にウィンドウがある場合のみ状態リストから削除する
		// （最後の1ウィンドウの場合は削除せずに残すことで、プロセス終了時にそのウィンドウが保存される）
		if (this.WindowCount > 1) {
			var ws = this._stateStore.RootState.Windows.FirstOrDefault(x => x.WindowId == windowContext.WindowId);
			if (ws != null) {
				this._stateStore.RootState.Windows.Remove(ws);
			}
		}
	}

	/// <summary>
	/// 指定ウィンドウ以外のウィンドウ情報を取得する。
	/// </summary>
	/// <param name="currentWindowId">除外するウィンドウID</param>
	/// <returns>指定ウィンドウ以外のウィンドウのID・代表タブ名・タブ数のコレクション</returns>
	public IReadOnlyList<(Guid WindowId, string TabName, int TabCount)> GetOtherWindows(Guid currentWindowId) {
		return this._windows
			.Where(x => x.WindowId != currentWindowId)
			.Select(x => {
				var windowState = this._stateStore.RootState.Windows.FirstOrDefault(w => w.WindowId == x.WindowId);
				var tabCount = windowState?.TabIds.Count ?? 0;
				var selectedTabId = windowState?.SelectedTabId.Value;
				var representativeTab = this._stateStore.RootState.Tabs.FirstOrDefault(t => t.TabId == selectedTabId);
				// 選択されているタブがない場合は最初のタブを使用
				if (representativeTab == null && windowState?.TabIds.Count > 0) {
					var firstTabId = windowState.TabIds[0];
					representativeTab = this._stateStore.RootState.Tabs.FirstOrDefault(t => t.TabId == firstTabId);
				}

				return (x.WindowId, TabName: representativeTab?.DisplayName.Value ?? x.WindowId.ToString(), TabCount: tabCount);
			})
			.ToList();
	}

	/// <summary>
	/// タブを別のウィンドウへ移動する。
	/// 呼び出し前に元ウィンドウのTabIdsからは既に削除されていることを前提とする。
	/// ターゲットウィンドウのTabIdsに追加する。
	/// </summary>
	/// <param name="tabId">移動するタブID</param>
	/// <param name="sourceWindowId">元のウィンドウID（参考用、この段階では削除済み）</param>
	/// <param name="targetWindowId">移動先ウィンドウID</param>
	public void MoveTabToWindow(Guid tabId, Guid sourceWindowId, Guid targetWindowId) {
		var targetWindow = this._stateStore.RootState.Windows.FirstOrDefault(w => w.WindowId == targetWindowId);
		if (targetWindow == null) {
			return;
		}

		targetWindow.TabIds.Add(tabId);
	}

	/// <summary>
	/// タブを新しいウィンドウで開く。
	/// 呼び出し前に元ウィンドウのTabIdsからは既に削除されていることを前提とする。
	/// 新しいウィンドウ状態を作成し、そこにタブを追加する。
	/// リアクティブバインディングにより自動的にUIが更新される。
	/// </summary>
	/// <param name="tabId">新しいウィンドウで開くタブID</param>
	/// <param name="sourceWindowId">元のウィンドウID（参考用、この段階では削除済み）</param>
	public void MoveTabToNewWindow(Guid tabId, Guid sourceWindowId) {
		// 新しいウィンドウ状態を作成
		var newWindowState = this._rootProvider.GetRequiredService<WindowStateModel>();
		newWindowState.WindowId = Guid.NewGuid();

		newWindowState.TabIds.Add(tabId);
		this._stateStore.RootState.Windows.Add(newWindowState);
	}

	protected override void Dispose(bool disposing) {
		if (disposing) {
			foreach (var wc in this._windows) {
				wc.Dispose();
			}
			this._windows.Clear();
		}
		base.Dispose(disposing);
	}
}