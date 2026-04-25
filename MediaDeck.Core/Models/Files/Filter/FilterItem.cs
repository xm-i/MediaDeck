using System.Linq.Expressions;

using MediaDeck.Database.Tables;

namespace MediaDeck.Core.Models.Files.Filter;

/// <summary>
/// フィルター条件
/// </summary>
/// <remarks>
/// <see cref="FilterItemCreators.IFilterItemCreator"/>から生成する。
/// </remarks>
/// <remarks>
/// コンストラクタ
/// </remarks>
/// <param name="condition">フィルタリング条件（SQL翻訳可能なExpression）</param>
public class FilterItem(Expression<Func<MediaItem, bool>> condition) {
	/// <summary>
	/// フィルタリング条件
	/// </summary>
	public Expression<Func<MediaItem, bool>> Condition {
		get;
	} = condition;

	public override string ToString() {
		return $"<[{base.ToString()}] {this.Condition}>";
	}
}