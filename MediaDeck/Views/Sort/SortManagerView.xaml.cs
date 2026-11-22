using MediaDeck.ViewModels.Sort;

namespace MediaDeck.Views.Sort;

/// <summary>
/// ソート条件管理ビュー。
/// </summary>
public sealed partial class SortManagerView {
	/// <summary>
	/// <see cref="SortManagerView"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	public SortManagerView() {
		this.InitializeComponent();
	}
}

/// <summary>
/// <see cref="SortManagerView"/> の基底クラス。
/// </summary>
public class SortManagerViewUserControl : UserControlBase<SortManagerViewModel>;