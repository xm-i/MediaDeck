using MediaDeck.ViewModels.Filters.FilterItemCreators;

namespace MediaDeck.Views.Filters.FilterItemCreators;

/// <summary>
/// 存在フィルター入力用コントロール。
/// </summary>
public partial class ExistsFilter {
	/// <summary>
	/// <see cref="ExistsFilter"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	public ExistsFilter() {
		this.InitializeComponent();
	}
}

/// <summary>
/// <see cref="ExistsFilter"/> の基底クラス。
/// </summary>
public class ExistsFilterControlBase : UserControlBase<ExistsFilterCreatorViewModel>;