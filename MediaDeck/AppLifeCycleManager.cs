using Microsoft.Windows.AppLifecycle;

namespace MediaDeck;

/// <summary>
/// アプリケーションのライフサイクルと単一インスタンス制御を管理するクラス。
/// </summary>
public static class AppLifeCycleManager {
	/// <summary>
	/// 単一インスタンスの登録を試みる。
	/// 既にメインインスタンスが存在する場合はリダイレクトして false を返す。
	/// </summary>
	public static bool TrySetAsMainInstance() {
		const string appKey = "MediaDeck-SingleInstance-Key";

		// 現在のインスタンス情報を取得
		var currentInstance = AppInstance.FindOrRegisterForKey(appKey);

		if (!currentInstance.IsCurrent) {
			// 別のインスタンスが既にメインとして登録済み
			// 起動引数をリダイレクト
			var activationArgs = AppInstance.GetCurrent().GetActivatedEventArgs();

			// 同期的にリダイレクト（Waitを使用）
			currentInstance.RedirectActivationToAsync(activationArgs).AsTask().Wait();
			return false;
		}

		// メインインスタンスとして登録 → リダイレクト受信ハンドラを設定
		currentInstance.Activated += OnRedirectedActivation;
		return true;
	}

	/// <summary>
	/// 他プロセスからのリダイレクトアクティベーションを受信するハンドラ。
	/// </summary>
	private static void OnRedirectedActivation(object? sender, AppActivationArguments e) {
		// このイベントはバックグラウンドスレッドで発火するため、
		// UIスレッドにディスパッチする必要がある
		if (App.Current is App app) {
			app.HandleRedirectedActivation(e);
		}
	}
}