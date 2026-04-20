using MediaDeck.Database.Tables;

namespace MediaDeck.Core.Models.Files.Filter;

public static class FilterExtensions {
	/// <summary>
	/// フィルターマネージャーで選択したフィルターを引数に渡されたクエリに適用して返却する。
	/// </summary>
	/// <param name="query">絞り込みクエリを適用するクエリ</param>
	/// <param name="filter">適用するフィルター</param>
	/// <returns>フィルター適用後クエリ</returns>
	public static IQueryable<MediaFile> Where(this IQueryable<MediaFile> query, FilterSelector filter) {
		return filter.SetFilterConditions(query);
	}
}