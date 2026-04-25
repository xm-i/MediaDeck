using System.Linq.Expressions;
using MediaDeck.Common.Utilities;
using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Database.Tables;
using R3.JsonConfig.Attributes;

namespace MediaDeck.Core.Models.Files.SearchConditions;

[GenerateR3JsonConfigDto]
[JsonConfigDerivedType("tag")]
[Inject(InjectServiceLifetime.Transient)]
public class TagSearchCondition : ISearchCondition {
	private readonly ITagsManager _tagsManager;

	public TagSearchCondition(ITagsManager tagsManager) {
		this._tagsManager = tagsManager;
	}

	public int TagId {
		get;
		set;
	}

	public string? RepresentativeText {
		get;
		set;
	}

	private ITagModel TargetTag {
		get {
			return field ??= this._tagsManager.Tags.FirstOrDefault(t => t.TagId == this.TagId) ?? throw new InvalidOperationException($"Tag with Id {this.TagId} not found.");
		}
	}

	public string DisplayText {
		get {
			try {
				var rep = this.RepresentativeText;
				if (rep != null) {
					rep = $" ({rep})";
				}
				return $"TagName={this.TargetTag.TagName}{rep}";
			} catch (InvalidOperationException) {
				return $"TagId={this.TagId}";
			}
		}
	}

	public Expression<Func<MediaFile, bool>>? WherePredicate {
		get {
			Expression<Func<MediaFile, bool>> exp1 =
				mediaFile => mediaFile.MediaFileTags.Select(x => x.TagId).Contains(this.TagId);
			var exp = exp1.Body;
			var visitor = new ParameterVisitor(exp1.Parameters);

			return Expression.Lambda<Func<MediaFile, bool>>(exp,
				visitor.Parameters);
		}
	}

	public bool IsMatchForSuggest(string searchWord) {
		if (this.TargetTag.TagName.Contains(searchWord) ||
			(this.TargetTag.Ruby?.Contains(searchWord) ?? false) ||
			(this.TargetTag.Romaji?.Contains(searchWord, StringComparison.CurrentCultureIgnoreCase) ?? false)) {
			this.RepresentativeText = null;
			return true;
		}
		var result = this.TargetTag.TagAliases
			.FirstOrDefault(x =>
				x.Alias.Contains(searchWord, StringComparison.CurrentCultureIgnoreCase) ||
				(x.Ruby?.Contains(searchWord) ?? false) ||
				(x.Romaji?.Contains(searchWord, StringComparison.CurrentCultureIgnoreCase) ?? false));
		this.RepresentativeText = result?.Alias;
		return result != null;
	}
}