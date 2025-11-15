using MediaDeck.Composition.Bases;
using MediaDeck.ViewModels.Filters.FilterItemCreators;

namespace MediaDeck.Views.Filters.FilterItemCreators;
/// <summary>
/// MediaTypeFilter.xaml の相互作用ロジック
/// </summary>
public partial class MediaTypeFilter: MediaTypeFilterPageBase {
	public MediaTypeFilter() {
		this.InitializeComponent();
	}
}

public class MediaTypeFilterPageBase : PageBase<MediaTypeFilterCreatorViewModel> {
}
