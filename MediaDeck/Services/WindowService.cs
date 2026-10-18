using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

using WinRT.Interop;

namespace MediaDeck.Services;

/// <summary>
/// ウィンドウ操作を提供するサービス実装クラス。
/// </summary>
[Inject(InjectServiceLifetime.Singleton, typeof(IWindowService))]
[Inject(InjectServiceLifetime.Singleton)]
public class WindowService(WindowManager windowManager, IServiceProvider serviceProvider) : IWindowService {
	private readonly WindowManager _windowManager = windowManager;
	private readonly IServiceProvider _serviceProvider = serviceProvider;

	/// <inheritdoc />
	public void ActivateCenteredOnMainWindow(Window window, Window parentWindow) {
		this.CenterWindowOnMainWindow(parentWindow, window);
		window.Activate();
	}

	/// <inheritdoc />
	public bool ActivateCentered<TWindow>(UIElement contextElement, Action<TWindow>? configure = null) where TWindow : Window {
		var parent = this._windowManager.GetWindowFromElement(contextElement);
		if (parent == null) {
			return false;
		}
		var window = this._serviceProvider.GetRequiredService<TWindow>();
		configure?.Invoke(window);
		this.ActivateCenteredOnMainWindow(window, parent);
		return true;
	}

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

	private static AppWindow? GetAppWindow(Window window) {
		var hWnd = WindowNative.GetWindowHandle(window);
		var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
		return AppWindow.GetFromWindowId(windowId);
	}
}