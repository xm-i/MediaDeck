using MediaDeck.Utils.Enums;
using MediaDeck.Models.Files.Filter.FilterItemObjects;

namespace MediaDeck.Models.Files.Filter.FilterItemCreators;
/// <summary>
/// 解像度フィルタークリエイター
/// </summary>
[AddTransient]
public class ResolutionFilterItemCreator : IFilterItemCreator<ResolutionFilterItemObject> {
	/// <summary>
	/// フィルター条件の作成
	/// </summary>
	/// <returns>作成された条件</returns>
	public FilterItem Create(ResolutionFilterItemObject filterItemObject) {
		var op = SearchTypeConverters.SearchTypeToFunc<double?>(filterItemObject.SearchType);
		if (filterItemObject.Width is { } w) {
			return new FilterItem(
				x => op(x.Width, w),
				x => op(x.Resolution?.Width, w),
				false);
		} else if (filterItemObject.Height is { } h) {
			return new FilterItem(
				x => op(x.Height, h),
				x => op(x.Resolution?.Height, h),
				false);
		} else if (filterItemObject.Resolution is { } r) {
			return new FilterItem(
				x => op(x.Width * x.Height, r.Area),
				x => op(x.Resolution?.Area, r.Area),
				false);
		}
		throw new InvalidOperationException();
	}
}
