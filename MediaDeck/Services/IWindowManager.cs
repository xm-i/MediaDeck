using MediaDeck.Composition.Interfaces.Services;

namespace MediaDeck.Services;


/// <summary>
/// アプリケーション内のウィンドウライフサイクルを管理するサービス。
/// ウィンドウの生成・破棄・一覧管理を担当する。
/// </summary>
public interface IWindowManager {
	/// <summary>
	/// 現在開いているウィンドウの数
	/// </summary>
	public int WindowCount {
		get;
	}

	/// <summary>
	/// 保存された状態からすべてのウィンドウを復元する。
	/// </summary>
	public void RestoreWindows();

	/// <summary>
	/// 指定ウィンドウを閉じる
	/// </summary>
	public void CloseWindow(IWindowContext windowContext);
}