using MediaDeck.ViewModels.Filters.FilterItemCreators;

namespace MediaDeck.Views.Filters.FilterItemCreators;

/// <summary>
/// FilePathFilter.xaml の相互作用ロジック
/// </summary>
public partial class FilePathFilter {
	public FilePathFilter() {
		this.InitializeComponent();
	}
}

/// <summary>
/// ファイルパスフィルターページの基底クラス
/// </summary>
public class FilePathFilterPageBase : PageBase<FilePathFilterCreatorViewModel>;