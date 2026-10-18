using Microsoft.UI.Xaml;

namespace MediaDeck.Services;

/// <summary>
/// ウィンドウのアクティブ化・センタリング操作を提供するサービスインターフェース。
/// </summary>
public interface IWindowService {
	/// <summary>
	/// 指定したウィンドウを親ウィンドウの中央に配置してアクティブにします。
	/// </summary>
	/// <param name="window">対象のウィンドウ。</param>
	/// <param name="parentWindow">親となるウィンドウ。</param>
	public void ActivateCenteredOnMainWindow(Window window, Window parentWindow);

	/// <summary>
	/// 指定した型のウィンドウを生成し、親ウィンドウの中央に配置してアクティブにします。
	/// </summary>
	/// <typeparam name="TWindow">ウィンドウの型。</typeparam>
	/// <param name="contextElement">親ウィンドウ特定用の UI 要素。</param>
	/// <param name="configure">ウィンドウ表示前に行う初期化アクション。</param>
	/// <returns>アクティブ化に成功した場合は <c>true</c>、親ウィンドウが見つからなかった場合は <c>false</c>。</returns>
	public bool ActivateCentered<TWindow>(UIElement contextElement, Action<TWindow>? configure = null) where TWindow : Window;
}