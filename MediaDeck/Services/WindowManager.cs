using System.Collections.Generic;
using MediaDeck.Common.Base;
using MediaDeck.Composition.Interfaces.Services;
using MediaDeck.Composition.Objects;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Core.Stores.State;
using MediaDeck.Views;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using ObservableCollections;

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
[Inject(InjectServiceLifetime.Singleton, typeof(IWindowManager))]
public class WindowManager : DisposableBase, IWindowManager {
	private readonly IServiceProvider _rootProvider;
	private readonly IStateStore _stateStore;
	private readonly SaveStateService _saveStateService;
	private readonly ILogger<WindowManager> _logger;
	private readonly List<WindowContext> _windows = [];

	public int WindowCount {
		get {
			return this._windows.Count;
		}
	}

	public WindowManager(
		IServiceProvider rootProvider,
		IStateStore stateStore,
		SaveStateService saveStateService,
		ILogger<WindowManager> logger) {
		this._rootProvider = rootProvider;
		this._stateStore = stateStore;
		this._logger = logger;
		this._saveStateService = saveStateService;

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

		var newState = new WindowStateModel {
			WindowId = windowId ?? Guid.NewGuid()
		};
		this._stateStore.RootState.Windows.Add(newState);
	}

	/// <summary>
	/// 保存された状態からすべてのウィンドウを復元する。
	/// </summary>
	public void RestoreWindows() {
		var list = this._stateStore.RootState.Windows.ToList();
		if (list.Count == 0) {
			this.OnWindowStateAdded(new WindowStateModel());
			return;
		}
		foreach (var windowState in list) {
			this.OnWindowStateAdded(windowState);
		}
	}

	public void CloseWindow(IWindowContext windowContext) {
		var ws = this._stateStore.RootState.Windows.FirstOrDefault(x => x.WindowId == windowContext.WindowId);
		if (ws != null) {
			this._stateStore.RootState.Windows.Remove(ws);
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
		// UIから閉じられた場合は状態リストから削除する
		// これにより OnWindowStateRemoved が呼ばれ、コンテキストの破棄などが行われる
		this._saveStateService.RequestSave();
		var ws = this._stateStore.RootState.Windows.FirstOrDefault(x => x.WindowId == windowContext.WindowId);
		if (ws != null) {
			this._stateStore.RootState.Windows.Remove(ws);
		}

		if (this.WindowCount == 0) {
			Application.Current.Exit();
		}
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