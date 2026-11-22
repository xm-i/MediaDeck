using MediaDeck.ViewModels.Filters.FilterItemCreators;

namespace MediaDeck.Views.Filters.FilterItemCreators;

/// <summary>
/// 座標フィルター入力用コントロール。
/// </summary>
public partial class LocationFilter {
	/// <summary>
	/// <see cref="LocationFilter"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	public LocationFilter() {
		this.InitializeComponent();
	}
}

/// <summary>
/// <see cref="LocationFilter"/> の基底クラス。
/// </summary>
public class LocationFilterControlBase : UserControlBase<LocationFilterCreatorViewModel>;