using System.Linq.Expressions;

using MediaDeck.Database.Tables;
using MediaDeck.FileTypes.Base.Models.Interfaces;

namespace MediaDeck.Models.Files.SearchConditions;
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
