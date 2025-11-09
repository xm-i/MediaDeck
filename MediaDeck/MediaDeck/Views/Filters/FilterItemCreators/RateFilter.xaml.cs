using MediaDeck.Composition.Bases;
using MediaDeck.ViewModels.Filters.FilterItemCreators;

namespace MediaDeck.Views.Filters.FilterItemCreators;
/// <summary>
/// RateFilter.xaml の相互作用ロジック
/// </summary>
public partial class RateFilter: RateFilterUserPageBase {
	public RateFilter() {
		this.InitializeComponent();
	}
}

public class RateFilterUserPageBase : PageBase<RateFilterCreatorViewModel> {
}