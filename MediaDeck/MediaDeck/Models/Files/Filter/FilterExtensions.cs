using System.Collections.Generic;

using MediaDeck.Database.Tables;
using MediaDeck.FileTypes.Base.Models.Interfaces;

namespace MediaDeck.Models.Files.Filter;
public static class FilterExtensions {
	/// <summary>
	/// フィルターマネージャーで選択したフィルターを引数に渡されたクエリに適用して返却する。
	/// </summary>
	/// <param name="query">絞り込みクエリを適用するクエリ</param>
	/// <param name="filter">適用するフィルター</param>
	/// <returns>フィルター適用後クエリ</returns>
	public static IEnumerable<MediaFile> Where(this IQueryable<MediaFile> query, FilterSelector filter) {
		return filter.SetFilterConditions(query);
	}

	/// <summary>
	/// フィルターマネージャーで選択したフィルターを引数に渡されたシーケンスに適用して返却する。
	/// </summary>
	/// <param name="files">絞り込みを適用するシーケンス</param>
	/// <param name="filter">適用するフィルター</param>
	/// <returns>フィルター適用後シーケンス</returns>
	public static IEnumerable<IFileModel> Where(this IEnumerable<IFileModel> files, FilterSelector filter) {
		return filter.SetFilterConditions(files);
	}
}
