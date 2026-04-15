namespace MediaDeck.Composition.Interfaces.Services;

/// <summary>
/// ウィンドウ操作を提供するサービスインターフェース
/// </summary>
public interface IWindowService {
	/// <summary>
	/// 指定したウィンドウをメインウィンドウの中央に配置してアクティブにします。
	/// </summary>
	/// <param name="window">対象のウィンドウ (Microsoft.UI.Xaml.Window)</param>
	public void ActivateCenteredOnMainWindow(object window);
}
