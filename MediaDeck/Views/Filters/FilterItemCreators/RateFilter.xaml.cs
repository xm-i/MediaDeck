using MediaDeck.ViewModels.Filters.FilterItemCreators;

using Microsoft.UI.Xaml.Controls;

namespace MediaDeck.Views.Filters.FilterItemCreators;

/// <summary>
/// 評価フィルター入力用コントロール。
/// </summary>
public partial class RateFilter {
	/// <summary>
	/// <see cref="RateFilter"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	public RateFilter() {
		this.InitializeComponent();
	}

	private void RatingControl_ValueChanged(RatingControl sender, object args) {
		this.ViewModel?.UpdateRateFromRatingControl(sender.Value);
	}
}

/// <summary>
/// <see cref="RateFilter"/> の基底クラス。
/// </summary>
public class RateFilterControlBase : UserControlBase<RateFilterCreatorViewModel>;