using MediaDeck.Composition.Bases;
using MediaDeck.ViewModels.Filters.FilterItemCreators;

namespace MediaDeck.Views.Filters.FilterItemCreators;
/// <summary>
/// FilePathFilter.xaml の相互作用ロジック
/// </summary>
public partial class FilePathFilter: FlePathFilterPageBase {
	public FilePathFilter() {
		this.InitializeComponent();
	}
}

public class FlePathFilterPageBase : PageBase<FilePathFilterCreatorViewModel> {
}
