using System.Linq.Expressions;

using MediaDeck.Composition.Interfaces.FileTypes.Models;
using MediaDeck.Database.Tables;

namespace MediaDeck.Composition.Interfaces.Files;
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
