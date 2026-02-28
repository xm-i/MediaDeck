using CommunityToolkit.Mvvm.DependencyInjection;

using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

using MediaDeck.Views;

using Windows.Graphics;

namespace MediaDeck.Utils.Tools;
public static class WinUIUtility {
	public static Window GetParentWindow(this FrameworkElement element) {
		var parent = element.XamlRoot.Content;

		while (parent is FrameworkElement fe) {
			parent = fe.XamlRoot.Content;
		}

		return ((object)parent as Window)!;
	}

	public static void ActivateCenteredOnMainWindow(this Window childWindow) {
		CenterWindowOnMainWindow(childWindow);
		childWindow.Activate();
	}

	private static void CenterWindowOnMainWindow(Window childWindow) {
		var mainWindow = Ioc.Default.GetRequiredService<MainWindow>();
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

		childAppWindow.Move(new PointInt32(x, y));
	}

	private static AppWindow? GetAppWindow(Window window) {
		var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
		var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
		return AppWindow.GetFromWindowId(windowId);
	}
}