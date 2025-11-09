using System.Linq.Expressions;

using MediaDeck.Database.Tables;
using MediaDeck.FileTypes.Base.Models.Interfaces;

namespace MediaDeck.Models.Files.SearchConditions;
public class TagSearchCondition: ISearchCondition {
	[Obsolete("for serialize")]
	public TagSearchCondition() {
		this.TargetTag = null!;
	}
	public TagSearchCondition(TagModel targetTag) {
		this.TargetTag = targetTag;
	}

	public TagModel TargetTag {
		get;
		set;
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

			return Expression.Lambda<Func<MediaFile, bool>>(
				exp,
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
				((x.Ruby ?? x.Alias.KatakanaToHiragana()).HiraganaToRomaji()?.Contains(searchWord, StringComparison.CurrentCultureIgnoreCase) ?? false)
			);
		this.TargetTag.RepresentativeText.Value = result?.Alias;
		return result != null;
	}
}
