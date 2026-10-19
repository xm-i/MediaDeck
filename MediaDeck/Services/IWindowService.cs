using Microsoft.UI.Xaml;

namespace MediaDeck.Services;

/// <summary>
/// ウィンドウのアクティブ化・センタリング操作およびサブウィンドウのライフサイクル管理を提供するサービスインターフェース。
/// </summary>
public interface IWindowService {
	/// <summary>
	/// 指定したウィンドウを親ウィンドウの中央に配置してアクティブにします。
	/// </summary>
	/// <param name="window">対象のウィンドウ。</param>
	/// <param name="parentWindow">親となるウィンドウ。</param>
	public void ActivateCenteredOnMainWindow(Window window, Window parentWindow);

	/// <summary>
	/// 指定した型のサブウィンドウを取得または生成し、親ウィンドウの中央に配置してアクティブにします。
	/// 既にウィンドウが開いている場合は新規生成を行わず、既存ウィンドウを復元・最前面化します。
	/// </summary>
	/// <typeparam name="TWindow">サブウィンドウの型。</typeparam>
	/// <param name="contextElement">親ウィンドウ特定用の UI 要素。</param>
	/// <param name="configure">ウィンドウ表示前に行う初期化アクション（既存ウィンドウ再利用時も呼び出されます）。</param>
	/// <returns>アクティブ化に成功した場合は <c>true</c>、親ウィンドウが見つからなかった場合は <c>false</c>。</returns>
	public bool ActivateCentered<TWindow>(UIElement contextElement, Action<TWindow>? configure = null) where TWindow : Window;

	/// <summary>
	/// 指定した型のサブウィンドウを取得または生成し、親ウィンドウの中央に配置してアクティブにします。
	/// 既にウィンドウが開いている場合は新規生成を行わず、既存ウィンドウを復元・最前面化します。
	/// </summary>
	/// <typeparam name="TWindow">サブウィンドウの型。</typeparam>
	/// <param name="parentWindow">親ウィンドウ（null の場合はプライマリウィンドウまたは単独でアクティブ化）。</param>
	/// <param name="configure">ウィンドウ表示前に行う初期化アクション（既存ウィンドウ再利用時も呼び出されます）。</param>
	/// <returns>アクティブ化に成功した場合は <c>true</c>。</returns>
	public bool ActivateCentered<TWindow>(Window? parentWindow, Action<TWindow>? configure = null) where TWindow : Window;

	/// <summary>
	/// 現在開いている指定した型のサブウィンドウを取得します。開いていない場合は null を返します。
	/// </summary>
	/// <typeparam name="TWindow">サブウィンドウの型。</typeparam>
	/// <returns>開いているサブウィンドウのインスタンス。未オープン時は null。</returns>
	public TWindow? GetOpenSubWindow<TWindow>() where TWindow : Window;
}