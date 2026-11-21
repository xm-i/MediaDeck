using System.Runtime.InteropServices;
using System.Threading;
using MediaDeck.Composition.Constants;
using MediaDeck.Composition.Startup;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace MediaDeck;

/// <summary>
/// アプリケーションのエントリーポイントを提供します。
/// </summary>
public static partial class Program {
	/// <summary>
	/// WinUI 3 プロセスの実行要件を確認します。
	/// </summary>
	[LibraryImport("Microsoft.ui.xaml.dll")]
	private static partial void XamlCheckProcessRequirements();

	[STAThread]
	public static void Main(string[] args) {
		var runtimePathOptions = RuntimeArgumentsParser.Parse(args);
		if (runtimePathOptions.BaseDirectory != null) {
			FilePathConstants.OverrideBaseDirectory(runtimePathOptions.BaseDirectory);
		}

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