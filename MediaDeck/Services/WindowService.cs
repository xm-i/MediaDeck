using MediaDeck.Composition.Interfaces.Services;
using MediaDeck.Views;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

using WinRT.Interop;

namespace MediaDeck.Services;

/// <summary>
/// ウィンドウ操作を提供するサービス実装クラス
/// </summary>
[Inject(InjectServiceLifetime.Singleton, typeof(IWindowService))]
public class WindowService(IServiceProvider serviceProvider) : IWindowService {
	private readonly IServiceProvider _serviceProvider = serviceProvider;

	/// <inheritdoc />
	public void ActivateCenteredOnMainWindow(object window) {
		if (window is not Window childWindow) {
			return;
		}

		var mainWindow = this._serviceProvider.GetRequiredService<MainWindow>();
		this.CenterWindowOnMainWindow(mainWindow, childWindow);
		childWindow.Activate();
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
