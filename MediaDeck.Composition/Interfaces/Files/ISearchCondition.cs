using System.Linq.Expressions;

using MediaDeck.Composition.Interfaces.FileTypes.Models;
using MediaDeck.Database.Tables;

using R3.JsonConfig.Attributes;

namespace MediaDeck.Composition.Interfaces.Files;

[GenerateR3JsonConfigDto]
public interface ISearchCondition {
	public string DisplayText {
		get;
	}

	public Expression<Func<MediaFile, bool>>? WherePredicate {
		get;
	}

	public Func<IFileModel, bool>? Filter {
		get;
	}

	public bool IsMatchForSuggest(string searchWord);
}