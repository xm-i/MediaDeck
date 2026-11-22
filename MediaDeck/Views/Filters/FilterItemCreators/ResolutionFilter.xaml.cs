using MediaDeck.ViewModels.Filters.FilterItemCreators;

namespace MediaDeck.Views.Filters.FilterItemCreators;

/// <summary>
/// 解像度フィルター入力用コントロール。
/// </summary>
public partial class ResolutionFilter {
	/// <summary>
	/// <see cref="ResolutionFilter"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	public ResolutionFilter() {
		this.InitializeComponent();
	}
}

/// <summary>
/// <see cref="ResolutionFilter"/> の基底クラス。
/// </summary>
public class ResolutionFilterControlBase : UserControlBase<ResolutionFilterCreatorViewModel>;