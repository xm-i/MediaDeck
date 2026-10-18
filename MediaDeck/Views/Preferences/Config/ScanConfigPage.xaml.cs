using MediaDeck.ViewModels.Preferences.Config;

namespace MediaDeck.Views.Preferences.Config;

/// <summary>
/// スキャン設定ページ。
/// </summary>
public sealed partial class ScanConfigPage : ScanConfigPageBase {
	/// <summary>
	/// <see cref="ScanConfigPage"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	public ScanConfigPage() {
		this.InitializeComponent();
	}
}

/// <summary>
/// ScanConfigPage の基底クラス。PageBase を介して ViewModel プロパティを提供します。
/// </summary>
public abstract class ScanConfigPageBase : PageBase<ScanConfigPageViewModel>;