using MediaDeck.ViewModels;

namespace MediaDeck.Views.Panes.TabPanes;

/// <summary>
/// タブのコンテンツ領域（各ペインとグリッドスプリッターの配置）を表示するビュー。
/// </summary>
public sealed partial class TabContentPane {
	/// <summary>
	/// <see cref="TabContentPane"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	public TabContentPane() {
		this.InitializeComponent();
	}
}

/// <summary>
/// TabContentPane の基底クラス。UserControlBase を介して ViewModel プロパティを提供します。
/// </summary>
public abstract class TabContentPaneUserControl : UserControlBase<TabViewModel>;