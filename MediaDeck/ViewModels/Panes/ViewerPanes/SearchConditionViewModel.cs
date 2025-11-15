using MediaDeck.Composition.Bases;
using MediaDeck.Models.Files.SearchConditions;

namespace MediaDeck.ViewModels.Panes.ViewerPanes;
public class SearchConditionViewModel : ViewModelBase {
	public SearchConditionViewModel(ISearchCondition searchCondition) {
		this.SearchCondition = searchCondition;
		this.DisplayText = searchCondition.DisplayText;
	}

	public string DisplayText {
		get;
	}

	public ISearchCondition SearchCondition {
		get;
	}
}
