using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

using WinRT.Interop;

namespace MediaDeck.Services;

/// <summary>
/// ウィンドウ操作を提供するサービス実装クラス
/// </summary>
[Inject(InjectServiceLifetime.Singleton)]
public class WindowService() {

	/// <summary>
	/// 指定したウィンドウを親ウィンドウの中央に配置してアクティブにします。
	/// </summary>
	/// <param name="window">対象のウィンドウ</param>
	/// <param name="parentWindow">親となるウィンドウ。</param>
	public void ActivateCenteredOnMainWindow(Window window, Window parentWindow) {
		if (window is not Window childWindow) {
			return;
		}

		var targetParent = parentWindow;
		if (targetParent != null) {
			this.CenterWindowOnMainWindow(targetParent, childWindow);
		}
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