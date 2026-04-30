using System.Linq.Expressions;

using MediaDeck.Composition.Tables;

using R3.JsonConfig.Attributes;

namespace MediaDeck.Composition.Interfaces.Files;

[GenerateR3JsonConfigDto]
public interface ISearchCondition {
	public string DisplayText {
		get;
	}

	public Expression<Func<MediaItem, bool>>? WherePredicate {
		get;
	}

	public bool IsMatchForSuggest(string searchWord);
}