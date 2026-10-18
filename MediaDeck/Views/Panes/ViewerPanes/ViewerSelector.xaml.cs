using MediaDeck.ViewModels.Panes.ViewerPanes;

namespace MediaDeck.Views.Panes.ViewerPanes;

/// <summary>
/// ビューアセレクターペインのビュー。
/// </summary>
public sealed partial class ViewerSelector {
	/// <summary>
	/// <see cref="ViewerSelector"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	public ViewerSelector() {
		this.InitializeComponent();
	}
}

/// <summary>
/// ViewerSelector の基底クラス。UserControlBase を介して ViewModel プロパティを提供します。
/// </summary>
public abstract class ViewerSelectorUserControl : UserControlBase<ViewerSelectorViewModel>;