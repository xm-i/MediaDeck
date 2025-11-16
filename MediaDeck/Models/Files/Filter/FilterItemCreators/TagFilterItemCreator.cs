using MediaDeck.Composition.Enum;
using MediaDeck.Models.Files.Filter.FilterItemObjects;

namespace MediaDeck.Models.Files.Filter.FilterItemCreators;
/// <summary>
/// タグフィルタークリエイター
/// </summary>
[AddTransient]
public class TagFilterItemCreator : IFilterItemCreator<TagFilterItemObject> {
	/// <summary>
	/// フィルター条件の作成
	/// </summary>
	/// <returns>作成された条件</returns>
	public FilterItem Create(TagFilterItemObject filterItemObject) {
		return new FilterItem(
			x => x.MediaFileTags.Select(mft => mft.Tag.TagName).Contains(filterItemObject.TagName) == (filterItemObject.SearchType == SearchTypeInclude.Include),
			x => x.Tags.Any(x => x.TagName == filterItemObject.TagName) == (filterItemObject.SearchType == SearchTypeInclude.Include),
			false);
	}
}
