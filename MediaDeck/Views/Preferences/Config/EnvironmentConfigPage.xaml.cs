using MediaDeck.ViewModels.Preferences.Config;

namespace MediaDeck.Views.Preferences.Config;

/// <summary>
/// 環境設定ページ。
/// </summary>
public sealed partial class EnvironmentConfigPage : EnvironmentConfigPageBase {
	/// <summary>
	/// <see cref="EnvironmentConfigPage"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	public EnvironmentConfigPage() {
		this.InitializeComponent();
	}
}

/// <summary>
/// EnvironmentConfigPage の基底クラス。PageBase を介して ViewModel プロパティを提供します。
/// </summary>
public abstract class EnvironmentConfigPageBase : PageBase<EnvironmentConfigPageViewModel>;