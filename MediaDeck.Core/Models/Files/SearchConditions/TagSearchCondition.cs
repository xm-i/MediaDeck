using System.Linq.Expressions;
using MediaDeck.Common.Utilities;
using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Composition.Interfaces.FileTypes.Models;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Database.Tables;
using R3.JsonConfig.Attributes;

namespace MediaDeck.Core.Models.Files.SearchConditions;

[GenerateR3JsonConfigDto]
[JsonConfigDerivedType("tag")]
[Inject(InjectServiceLifetime.Transient)]
public class TagSearchCondition : ISearchCondition {
	public TagSearchCondition() {
	}

	public ITagModel TargetTag {
		get {
			return field ?? throw new InvalidOperationException($"{nameof(this.TargetTag)} is not initialized.");
		}
		set {
			field = value;
		}
	}

	public string DisplayText {
		get {
			return $"TagName={this.TargetTag.TagName}";
		}
	}

	public Expression<Func<MediaFile, bool>>? WherePredicate {
		get {
			Expression<Func<MediaFile, bool>> exp1 =
				mediaFile => mediaFile.MediaFileTags.Select(x => x.TagId).Contains(this.TargetTag.TagId);
			var exp = exp1.Body;
			var visitor = new ParameterVisitor(exp1.Parameters);

			return Expression.Lambda<Func<MediaFile, bool>>(exp,
				visitor.Parameters);
		}
	}

	public Func<IFileModel, bool>? Filter {
		get;
	} = null;

	public bool IsMatchForSuggest(string searchWord) {
		if (this.TargetTag.TagName.Contains(searchWord) ||
			(this.TargetTag.TagName.KatakanaToHiragana().HiraganaToRomaji()?.Contains(searchWord, StringComparison.CurrentCultureIgnoreCase) ?? false)) {
			this.TargetTag.RepresentativeText.Value = null;
			return true;
		}
		var result = this.TargetTag.TagAliases
			.FirstOrDefault(x =>
				x.Alias.Contains(searchWord, StringComparison.CurrentCultureIgnoreCase) ||
				(x.Ruby?.Contains(searchWord) ?? false) ||
				((x.Ruby ?? x.Alias.KatakanaToHiragana()).HiraganaToRomaji()?.Contains(searchWord, StringComparison.CurrentCultureIgnoreCase) ?? false));
		this.TargetTag.RepresentativeText.Value = result?.Alias;
		return result != null;
	}
}