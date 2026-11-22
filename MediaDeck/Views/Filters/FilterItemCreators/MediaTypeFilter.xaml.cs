using MediaDeck.ViewModels.Filters.FilterItemCreators;

namespace MediaDeck.Views.Filters.FilterItemCreators;

/// <summary>
/// メディアタイプフィルター入力用コントロール。
/// </summary>
public partial class MediaTypeFilter {
	/// <summary>
	/// <see cref="MediaTypeFilter"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	public MediaTypeFilter() {
		this.InitializeComponent();
	}
}

/// <summary>
/// <see cref="MediaTypeFilter"/> の基底クラス。
/// </summary>
public class MediaTypeFilterControlBase : UserControlBase<MediaTypeFilterCreatorViewModel>;