
using System.Linq.Expressions;

using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Composition.Interfaces.FileTypes.Models;
using MediaDeck.Database.Tables;

namespace MediaDeck.Models.Files.SearchConditions;
public class WordSearchCondition: ISearchCondition {
	[Obsolete("for serialize")]
	public WordSearchCondition() {
		this.Word = null!;
	}
	public WordSearchCondition(string word) {
		this.Word = word;
	}

	public string Word {
		get;
		set;
	}

	public string DisplayText {
		get {
			return $"Word={this.Word}";
		}
	}

	public Expression<Func<MediaFile, bool>>? WherePredicate {
		get {
			Expression<Func<MediaFile, bool>> exp1 =
			mediaFile =>
				EF.Functions.Like(mediaFile.FilePath, $"%{this.Word}%") ||
				EF.Functions.Like(mediaFile.Position!.DisplayName!, $"%{this.Word}%");
			var exp = exp1.Body;
			var visitor = new ParameterVisitor(exp1.Parameters);

			return Expression.Lambda<Func<MediaFile, bool>>(
				exp,
				visitor.Parameters);
		}
	}

	public Func<IFileModel, bool>? Filter {
		get;
	} = null;

	public bool IsMatchForSuggest(string searchWord) {
		return this.Word.Contains(searchWord);
	}
}
