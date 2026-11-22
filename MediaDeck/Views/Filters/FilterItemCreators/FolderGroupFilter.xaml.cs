using MediaDeck.ViewModels.Filters.FilterItemCreators;

namespace MediaDeck.Views.Filters.FilterItemCreators;

/// <summary>
/// フォルダグループフィルター入力用コントロール。
/// </summary>
public partial class FolderGroupFilter {
	/// <summary>
	/// <see cref="FolderGroupFilter"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	public FolderGroupFilter() {
		this.InitializeComponent();
	}
}

/// <summary>
/// <see cref="FolderGroupFilter"/> の基底クラス。
/// </summary>
public class FolderGroupFilterControlBase : UserControlBase<FolderGroupFilterCreatorViewModel>;