using MediaDeck.ViewModels.Filters.FilterItemCreators;

namespace MediaDeck.Views.Filters.FilterItemCreators;

/// <summary>
/// FolderGroupFilter.xaml の相互作用ロジック
/// </summary>
public partial class FolderGroupFilter {
	public FolderGroupFilter() {
		this.InitializeComponent();
	}
}

public class FolderGroupFilterPageBase : PageBase<FolderGroupFilterCreatorViewModel>;