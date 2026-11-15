using MediaDeck.ViewModels.Preferences.Config;

namespace MediaDeck.Views.Preferences.Config;

/// <summary>
/// 言語設定ページ。
/// </summary>
public sealed partial class LanguageConfigPage : LanguageConfigPageBase {
	/// <summary>
	/// <see cref="LanguageConfigPage"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	public LanguageConfigPage() {
		this.InitializeComponent();
	}
}

/// <summary>
/// LanguageConfigPage の基底クラス。PageBase を介して ViewModel プロパティを提供します。
/// </summary>
public abstract class LanguageConfigPageBase : PageBase<LanguageConfigPageViewModel>;