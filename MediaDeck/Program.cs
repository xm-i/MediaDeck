using System.Threading;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace MediaDeck;

public static class Program {
	[System.Runtime.InteropServices.DllImport("Microsoft.ui.xaml.dll")]
	private static extern void XamlCheckProcessRequirements();

	[STAThread]
	public static void Main(string[] args) {
		XamlCheckProcessRequirements();

		// 単一インスタンスチェック
		if (!AppLifeCycleManager.TrySetAsMainInstance()) {
			return;
		}

		Application.Start(static (p) => {
			var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
			SynchronizationContext.SetSynchronizationContext(context);
			_ = new App();
		});
	}
}