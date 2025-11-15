using MediaDeck.Composition.Bases;
using MediaDeck.ViewModels.Filters.FilterItemCreators;

namespace MediaDeck.Views.Filters.FilterItemCreators;
/// <summary>
/// ResolutionFilter.xaml の相互作用ロジック
/// </summary>
public partial class ResolutionFilter: ResolutionFilterPageBase {
	public ResolutionFilter() {
		this.InitializeComponent();
	}
}

public class ResolutionFilterPageBase : PageBase<ResolutionFilterCreatorViewModel> {
}