using System.Linq.Expressions;
using GenJsonConfig.Attributes;
using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Composition.Tables;
using MediaDeck.Core.Models.Tags;

namespace MediaDeck.Core.Models.Files.SearchConditions;

/// <summary>
/// タグによるメディアアイテムの検索条件
/// </summary>
[GenerateJsonConfigDto]
[JsonConfigDerivedType("tag")]
[Inject(InjectServiceLifetime.Transient)]
public class TagSearchCondition : ISearchCondition {
	private readonly ITagsManager _tagsManager;

	/// <summary>
	/// <see cref="TagSearchCondition"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="tagsManager">タグマネージャー</param>
	public TagSearchCondition(ITagsManager tagsManager) {
		this._tagsManager = tagsManager;
	}

	/// <summary>
	/// 検索対象のタグID
	/// </summary>
	public int TagId {
		get;
		set;
	}

	/// <summary>
	/// サジェストで一致したエイリアス表示用テキスト
	/// </summary>
	public string? RepresentativeText {
		get;
		set;
	}

	private ITagModel TargetTag {
		get {
			return this._tagsManager.Tags.FirstOrDefault(t => t.TagId == this.TagId)
				?? throw new InvalidOperationException($"Tag with Id {this.TagId} not found.");
		}
	}

	/// <inheritdoc />
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

	/// <inheritdoc />
	public Expression<Func<MediaItem, bool>>? WherePredicate {
		get {
			return mediaItem => mediaItem.MediaItemTags.Select(x => x.TagId).Contains(this.TagId);
		}
	}

	/// <inheritdoc />
	public bool IsMatchForSuggest(string searchWord) {
		var result = this.TargetTag.MatchesFilter(searchWord, out var representativeText);
		this.RepresentativeText = representativeText;
		return result;
	}
}