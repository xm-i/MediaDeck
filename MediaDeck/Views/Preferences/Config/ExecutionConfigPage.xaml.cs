using MediaDeck.ViewModels.Preferences.Config;

namespace MediaDeck.Views.Preferences.Config;

/// <summary>
/// 外部プログラム実行設定ページ。
/// </summary>
public sealed partial class ExecutionConfigPage : ExecutionConfigPageBase {
	/// <summary>
	/// <see cref="ExecutionConfigPage"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	public ExecutionConfigPage() {
		this.InitializeComponent();
	}
}

/// <summary>
/// ExecutionConfigPage の基底クラス。PageBase を介して ViewModel プロパティを提供します。
/// </summary>
public abstract class ExecutionConfigPageBase : PageBase<ExecutionConfigPageViewModel>;