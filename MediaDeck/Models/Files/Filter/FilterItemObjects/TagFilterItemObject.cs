using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.Files;

namespace MediaDeck.Models.Files.Filter.FilterItemObjects;
/// <summary>
/// タグフィルターアイテムオブジェクト
/// </summary>
public class TagFilterItemObject : IFilterItemObject {
	/// <summary>
	/// 表示名
	/// </summary>
	public string DisplayName {
		get {
			return $"{this.TagName} tag {(this.SearchType == SearchTypeInclude.Include ? "included" : "not included")}";
		}
	}

	/// <summary>
	/// タグ名
	/// </summary>
	public string TagName {
		get;
		set;
	}

	/// <summary>
	/// 検索タイプ
	/// </summary>
	public SearchTypeInclude SearchType {
		get;
		set;
	}

	[Obsolete("for serialize")]
	public TagFilterItemObject() {
		this.TagName = null!;
	}

	/// <summary>
	/// コンストラクタ
	/// </summary>
	/// <param name="tagName">タグ名</param>
	/// <param name="searchType">検索タイプ</param>
	public TagFilterItemObject(string tagName, SearchTypeInclude searchType) {
		if (tagName == null || !Enum.IsDefined(typeof(SearchTypeInclude), searchType)) {
			throw new ArgumentException();
		}
		this.TagName = tagName;
		this.SearchType = searchType;
	}
}
