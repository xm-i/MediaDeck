using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;

namespace MediaDeck.Views.Controls;

/// <summary>
/// サイドペイン共通の上部ヘッダーコントロール。
/// </summary>
[ContentProperty(Name = nameof(ActionContent))]
public sealed partial class PaneHeaderControl : UserControl {
	/// <summary>
	/// アイコングリフの依存関係プロパティ。
	/// </summary>
	public static readonly DependencyProperty GlyphProperty =
		DependencyProperty.Register(
			nameof(Glyph),
			typeof(string),
			typeof(PaneHeaderControl),
			new PropertyMetadata(string.Empty));

	/// <summary>
	/// タイトルの依存関係プロパティ。
	/// </summary>
	public static readonly DependencyProperty TitleProperty =
		DependencyProperty.Register(
			nameof(Title),
			typeof(string),
			typeof(PaneHeaderControl),
			new PropertyMetadata(string.Empty, (d, _) => ((PaneHeaderControl)d).UpdateDisplayText()));

	/// <summary>
	/// 表示テキスト（x:Uid用）の依存関係プロパティ。
	/// </summary>
	public static readonly DependencyProperty TextProperty =
		DependencyProperty.Register(
			nameof(Text),
			typeof(string),
			typeof(PaneHeaderControl),
			new PropertyMetadata(string.Empty, (d, _) => ((PaneHeaderControl)d).UpdateDisplayText()));

	/// <summary>
	/// 右側操作領域コンテンツの依存関係プロパティ。
	/// </summary>
	public static readonly DependencyProperty ActionContentProperty =
		DependencyProperty.Register(
			nameof(ActionContent),
			typeof(object),
			typeof(PaneHeaderControl),
			new PropertyMetadata(null));

	/// <summary>
	/// アイコングリフを取得または設定します。
	/// </summary>
	public string Glyph {
		get {
			return (string)this.GetValue(GlyphProperty);
		}

		set {
			this.SetValue(GlyphProperty, value);
		}
	}

	/// <summary>
	/// タイトルを取得または設定します。
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
	/// 表示テキストを取得または設定します（x:Uid によるローカライズに対応）。
	/// </summary>
	public string Text {
		get {
			return (string)this.GetValue(TextProperty);
		}

		set {
			this.SetValue(TextProperty, value);
		}
	}

	/// <summary>
	/// 右側操作領域のコンテンツを取得または設定します。
	/// </summary>
	public object? ActionContent {
		get {
			return this.GetValue(ActionContentProperty);
		}

		set {
			this.SetValue(ActionContentProperty, value);
		}
	}

	/// <summary>
	/// <see cref="PaneHeaderControl"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	public PaneHeaderControl() {
		this.InitializeComponent();
		this.Loaded += (_, _) => this.UpdateDisplayText();
	}

	private void UpdateDisplayText() {
		this.TitleTextBlock?.Text = !string.IsNullOrEmpty(this.Text) ? this.Text : this.Title;
	}
}