using System.Collections.Generic;
using System.Runtime.InteropServices;
using MediaDeck.Common.Base;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace MediaDeck.Services;

/// <summary>
/// ウィンドウ操作およびサブウィンドウのライフサイクル管理を提供するサービス実装クラス。
/// </summary>
[Inject(InjectServiceLifetime.Singleton, typeof(IWindowService))]
[Inject(InjectServiceLifetime.Singleton)]
public class WindowService(WindowManager windowManager, IServiceProvider serviceProvider) : ServiceBase, IWindowService {
	private readonly WindowManager _windowManager = windowManager;
	private readonly IServiceProvider _serviceProvider = serviceProvider;

	private readonly Dictionary<Type, Window> _openWindows = [];
	private readonly Dictionary<Window, HashSet<Window>> _parentToChildren = [];
	private readonly Dictionary<Window, Window> _childToParent = [];
	private readonly HashSet<Window> _trackedParents = [];

	/// <inheritdoc />
	public void ActivateCenteredOnMainWindow(Window window, Window parentWindow) {
		this.RegisterAndShowSubWindow(window.GetType(), window, parentWindow);
	}

	/// <inheritdoc />
	public bool ActivateCentered<TWindow>(UIElement contextElement, Action<TWindow>? configure = null) where TWindow : Window {
		var parent = this._windowManager.GetWindowFromElement(contextElement);
		if (parent == null) {
			return false;
		}
		return this.ActivateCentered<TWindow>(parent, configure);
	}

	/// <inheritdoc />
	public bool ActivateCentered<TWindow>(Window? parentWindow, Action<TWindow>? configure = null) where TWindow : Window {
		if (parentWindow == null) {
			var windows = this._windowManager.Windows;
			if (windows.Count > 0) {
				parentWindow = windows[0].Window;
			}
		}

		var windowType = typeof(TWindow);
		if (this._openWindows.TryGetValue(windowType, out var existingWindow) && existingWindow is TWindow typedExisting) {
			// 既に開いている場合はパラメータを更新し、必要に応じてオーナーを更新して最前面化
			configure?.Invoke(typedExisting);

			if (parentWindow != null) {
				this.SetOwner(typedExisting, parentWindow);
				this.TrackParentChild(parentWindow, typedExisting);
			}

			RestoreAndActivate(typedExisting);
			return true;
		}

		var newWindow = this._serviceProvider.GetRequiredService<TWindow>();
		configure?.Invoke(newWindow);
		this.RegisterAndShowSubWindow(windowType, newWindow, parentWindow);
		return true;
	}

	/// <inheritdoc />
	public TWindow? GetOpenSubWindow<TWindow>() where TWindow : Window {
		return this._openWindows.TryGetValue(typeof(TWindow), out var window) && window is TWindow typedWindow
			? typedWindow
			: null;
	}

	/// <summary>
	/// 新規サブウィンドウを登録し、オーナー設定・中央配置を行って表示します。
	/// </summary>
	private void RegisterAndShowSubWindow(Type windowType, Window childWindow, Window? parentWindow) {
		this._openWindows[windowType] = childWindow;

		childWindow.Closed += (_, _) => {
			this.OnSubWindowClosed(windowType, childWindow);
		};

		HideFromTaskbar(childWindow);

		if (parentWindow != null) {
			this.SetOwner(childWindow, parentWindow);
			this.TrackParentChild(parentWindow, childWindow);
			this.CenterWindowOnMainWindow(parentWindow, childWindow);
		}

		childWindow.Activate();
	}

	/// <summary>
	/// 親ウィンドウと子ウィンドウのリレーションを追跡し、親が閉じられた際の連動クローズを設定します。
	/// </summary>
	private void TrackParentChild(Window parentWindow, Window childWindow) {
		// 既に別の親に紐付いていた場合は旧親から削除
		if (this._childToParent.TryGetValue(childWindow, out var oldParent) && oldParent != parentWindow) {
			if (this._parentToChildren.TryGetValue(oldParent, out var oldChildren)) {
				oldChildren.Remove(childWindow);
			}
		}

		this._childToParent[childWindow] = parentWindow;

		if (!this._parentToChildren.TryGetValue(parentWindow, out var children)) {
			children = [];
			this._parentToChildren[parentWindow] = children;
		}
		children.Add(childWindow);

		if (this._trackedParents.Add(parentWindow)) {
			parentWindow.Closed += (_, _) => {
				this.OnParentWindowClosed(parentWindow);
			};
		}
	}

	/// <summary>
	/// 親ウィンドウが閉じられた際に、紐づくサブウィンドウを連動して閉じます。
	/// </summary>
	private void OnParentWindowClosed(Window parentWindow) {
		this._trackedParents.Remove(parentWindow);

		if (this._parentToChildren.Remove(parentWindow, out var children)) {
			foreach (var child in children.ToList()) {
				this._childToParent.Remove(child);
				child.Close();
			}
		}
	}

	/// <summary>
	/// サブウィンドウが閉じられた際に管理ディクショナリから除外します。
	/// </summary>
	private void OnSubWindowClosed(Type windowType, Window childWindow) {
		this._openWindows.Remove(windowType);

		if (this._childToParent.Remove(childWindow, out var parent)) {
			if (this._parentToChildren.TryGetValue(parent, out var children)) {
				children.Remove(childWindow);
			}
		}
	}

	/// <summary>
	/// 子ウィンドウのオーナーとして親ウィンドウのハンドルを設定します。
	/// </summary>
	private void SetOwner(Window childWindow, Window parentWindow) {
		var childHwnd = WindowNative.GetWindowHandle(childWindow);
		var parentHwnd = WindowNative.GetWindowHandle(parentWindow);

		if (childHwnd != nint.Zero && parentHwnd != nint.Zero) {
			NativeMethods.SetWindowLongPtr(childHwnd, NativeMethods.GWLP_HWNDPARENT, parentHwnd);
		}
	}

	/// <summary>
	/// ウィンドウを親ウィンドウの中央に配置します。
	/// </summary>
	private void CenterWindowOnMainWindow(Window mainWindow, Window childWindow) {
		var mainAppWindow = GetAppWindow(mainWindow);
		var childAppWindow = GetAppWindow(childWindow);

		if (mainAppWindow is null || childAppWindow is null) {
			return;
		}

		var mainPos = mainAppWindow.Position;
		var mainSize = mainAppWindow.Size;
		var childSize = childAppWindow.Size;

		var x = mainPos.X + ((mainSize.Width - childSize.Width) / 2);
		var y = mainPos.Y + ((mainSize.Height - childSize.Height) / 2);

		childAppWindow.Move(new(x, y));
	}

	/// <summary>
	/// ウィンドウをタスクバーおよび Alt+Tab スイッチャーから非表示にします。
	/// </summary>
	private static void HideFromTaskbar(Window window) {
		var appWindow = GetAppWindow(window);
		appWindow?.IsShownInSwitchers = false;
	}

	/// <summary>
	/// 最小化されているウィンドウを復元し、フォアグラウンドに最前面化・アクティブ化します。
	/// </summary>
	private static void RestoreAndActivate(Window window) {
		var appWindow = GetAppWindow(window);
		if (appWindow?.Presenter is OverlappedPresenter presenter) {
			if (presenter.State == OverlappedPresenterState.Minimized) {
				presenter.Restore();
			}
		}

		var hWnd = WindowNative.GetWindowHandle(window);
		if (hWnd != nint.Zero) {
			NativeMethods.SetForegroundWindow(hWnd);
		}

		window.Activate();
	}

	/// <summary>
	/// <see cref="Window"/> から <see cref="AppWindow"/> を取得します。
	/// </summary>
	/// <param name="window">対象のウィンドウ。</param>
	/// <returns>対応する <see cref="AppWindow"/>。取得できない場合は null。</returns>
	private static AppWindow? GetAppWindow(Window window) {
		var hWnd = WindowNative.GetWindowHandle(window);
		var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
		return AppWindow.GetFromWindowId(windowId);
	}

	protected override void Dispose(bool disposing) {
		if (disposing) {
			foreach (var window in this._openWindows.Values.ToList()) {
				window.Close();
			}
			this._openWindows.Clear();
			this._parentToChildren.Clear();
			this._childToParent.Clear();
			this._trackedParents.Clear();
		}
		base.Dispose(disposing);
	}

	private static class NativeMethods {
		[DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
		private static extern nint SetWindowLongPtr64(nint hWnd, int nIndex, nint dwNewLong);

		[DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
		private static extern int SetWindowLong32(nint hWnd, int nIndex, int dwNewLong);

		public static nint SetWindowLongPtr(nint hWnd, int nIndex, nint dwNewLong) {
			return IntPtr.Size == 8
				? SetWindowLongPtr64(hWnd, nIndex, dwNewLong)
				: SetWindowLong32(hWnd, nIndex, (int)dwNewLong);
		}

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SetForegroundWindow(nint hWnd);

		public const int GWLP_HWNDPARENT = -8;
	}
}