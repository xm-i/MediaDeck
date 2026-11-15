using MediaDeck.ViewModels.Preferences.Config;

namespace MediaDeck.Views.Preferences.Config;

/// <summary>
/// サムネイル設定ページ。
/// </summary>
public sealed partial class ThumbnailConfigPage : ThumbnailConfigPageBase {
	/// <summary>
	/// <see cref="ThumbnailConfigPage"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	public ThumbnailConfigPage() {
		this.InitializeComponent();
	}
}

/// <summary>
/// ThumbnailConfigPage の基底クラス。PageBase を介して ViewModel プロパティを提供します。
/// </summary>
public abstract class ThumbnailConfigPageBase : PageBase<ThumbnailConfigPageViewModel>;