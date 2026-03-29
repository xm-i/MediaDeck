using MediaDeck.ViewModels.Filters.FilterItemCreators;

namespace MediaDeck.Views.Filters.FilterItemCreators;

/// <summary>
/// LocationFilter.xaml の相互作用ロジック
/// </summary>
public partial class LocationFilter {
	public LocationFilter() {
		this.InitializeComponent();
	}
}

public class LocationFilterPageBase : PageBase<LocationFilterCreatorViewModel>;