using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Common.Base;

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
