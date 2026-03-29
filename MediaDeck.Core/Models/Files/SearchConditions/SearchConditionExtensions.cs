using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Composition.Interfaces.FileTypes.Models;
using MediaDeck.Core.Models.Files.SearchConditions;
using MediaDeck.Database.Tables;

namespace MediaDeck.Core.Models.Files.SearchConditions;

public static class SearchConditionExtensions {
	/// <summary>
	/// 検索条件を引数に渡されたクエリに適用して返却する。
	/// </summary>
	/// <param name="query">絞り込みクエリを適用するクエリ</param>
	/// <param name="conditions">適用する検索条件</param>
	/// <returns>検索条件適用後クエリ</returns>
	public static IQueryable<MediaFile> Where(this IQueryable<MediaFile> query, IEnumerable<ISearchCondition> conditions) {
		foreach (var condition in conditions.Where(x => x.WherePredicate != null)) {
			query = query.Where(condition.WherePredicate!);
		}
		return query;
	}

	/// <summary>
	/// 検索条件を引数に渡されたシーケンスに適用して返却する。
	/// </summary>
	/// <param name="files">絞り込みを適用するシーケンス</param>
	/// <param name="conditions">適用する検索条件</param>
	/// <returns>検索条件適用後シーケンス</returns>
	public static IEnumerable<IFileModel> Where(this IEnumerable<IFileModel> files, IEnumerable<ISearchCondition> conditions) {
		foreach (var condition in conditions.Where(x => x.Filter != null)) {
			files = files.Where(condition.Filter!);
		}
		return files;
	}
}