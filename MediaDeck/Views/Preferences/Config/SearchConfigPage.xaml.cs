using MediaDeck.ViewModels.Preferences.Config;

namespace MediaDeck.Views.Preferences.Config;

/// <summary>
/// 検索設定ページ。
/// </summary>
public sealed partial class SearchConfigPage : SearchConfigPageBase {
	/// <summary>
	/// <see cref="SearchConfigPage"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	public SearchConfigPage() {
		this.InitializeComponent();
	}
}

/// <summary>
/// SearchConfigPage の基底クラス。PageBase を介して ViewModel プロパティを提供します。
/// </summary>
public abstract class SearchConfigPageBase : PageBase<SearchConfigPageViewModel>;