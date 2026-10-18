using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MediaDeck.Views.Controls;

/// <summary>
/// サブウィンドウ共通のカスタムタイトルバーコントロール。
/// </summary>
public sealed partial class WindowTitleBar : UserControl {
	/// <summary>
	/// タイトルプロパティの依存関係プロパティ。
	/// </summary>
	public static readonly DependencyProperty TitleProperty =
		DependencyProperty.Register(
			nameof(Title),
			typeof(string),
			typeof(WindowTitleBar),
			new PropertyMetadata(string.Empty));

	/// <summary>
	/// ウィンドウタイトルを取得または設定します。
	/// </summary>
	public string Title {
		get {
			return (string)this.GetValue(TitleProperty);
		}

		set {
			this.SetValue(TitleProperty, value);
		}
	}

	/// <summary>
	/// <see cref="WindowTitleBar"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	public WindowTitleBar() {
		this.InitializeComponent();
	}
}