using System.Linq.Expressions;

using MediaDeck.Common.Utilities;
using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Composition.Tables;

using R3.JsonConfig.Attributes;

namespace MediaDeck.Core.Models.Files.SearchConditions;

[GenerateR3JsonConfigDto]
[JsonConfigDerivedType("word")]
[Inject(InjectServiceLifetime.Transient)]
public class WordSearchCondition : ISearchCondition {
	public WordSearchCondition() {
	}

	public string Word {
		get {
			return field ?? throw new InvalidOperationException($"{nameof(this.Word)} is not initialized.");
		}

		set {
			field = value;
		}
	}

	public string DisplayText {
		get {
			return $"Word={this.Word}";
		}
	}

	public Expression<Func<MediaItem, bool>>? WherePredicate {
		get {
			Expression<Func<MediaItem, bool>> exp1 =
				MediaItem =>
					EF.Functions.Like(MediaItem.FilePath, $"%{this.Word}%") ||
					EF.Functions.Like(MediaItem.Position!.DisplayName!, $"%{this.Word}%");
			var exp = exp1.Body;
			var visitor = new ParameterVisitor(exp1.Parameters);

			return Expression.Lambda<Func<MediaItem, bool>>(exp,
				visitor.Parameters);
		}
	}

	public bool IsMatchForSuggest(string searchWord) {
		return this.Word.Contains(searchWord);
	}
}