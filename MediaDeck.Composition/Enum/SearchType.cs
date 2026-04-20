using System.Linq.Expressions;

namespace MediaDeck.Composition.Enum;

/// <summary>
/// 検索タイプ(含む/含まない)
/// </summary>
public enum SearchTypeInclude {
	/// <summary>
	/// 含むものを検索
	/// </summary>
	Include,

	/// <summary>
	/// 含まないものを検索
	/// </summary>
	Exclude
}

/// <summary>
/// 検索タイプ(比較)
/// </summary>
public enum SearchTypeComparison {
	/// <summary>
	/// 超
	/// </summary>
	GreaterThan,

	/// <summary>
	/// 以上
	/// </summary>
	GreaterThanOrEqual,

	/// <summary>
	/// 同値
	/// </summary>
	Equal,

	/// <summary>
	/// 以下
	/// </summary>
	LessThanOrEqual,

	/// <summary>
	/// 未満
	/// </summary>
	LessThan
}

public static class SearchTypeConverters {
	/// <summary>
	/// 検索タイプ毎に適切な比較メソッドを返却する。
	/// </summary>
	/// <typeparam name="T">比較する型</typeparam>
	/// <param name="searchType">検索タイプ</param>
	/// <returns>比較用メソッド</returns>
	public static Func<T, T, bool> SearchTypeToFunc<T>(SearchTypeComparison searchType) {
		var op = GetBinaryExpressionFactory(searchType);

		var p1 = Expression.Parameter(typeof(T));
		var p2 = Expression.Parameter(typeof(T));
		var func = Expression.Lambda<Func<T, T, bool>>(op(p1, p2), p1, p2);

		return func.Compile();
	}

	/// <summary>
	/// 検索タイプに対応するBinaryExpression生成関数を返却する。
	/// </summary>
	/// <param name="searchType">検索タイプ</param>
	/// <returns>BinaryExpression生成関数</returns>
	public static Func<Expression, Expression, BinaryExpression> GetBinaryExpressionFactory(SearchTypeComparison searchType) {
		return searchType switch {
			SearchTypeComparison.GreaterThan => Expression.GreaterThan,
			SearchTypeComparison.GreaterThanOrEqual => Expression.GreaterThanOrEqual,
			SearchTypeComparison.Equal => Expression.Equal,
			SearchTypeComparison.LessThanOrEqual => Expression.LessThanOrEqual,
			SearchTypeComparison.LessThan => Expression.LessThan,
			_ => throw new ArgumentOutOfRangeException(nameof(searchType))
		};
	}

	/// <summary>
	/// 検索タイプに対応する比較Expression（MediaFileのプロパティと定数値の比較）を生成する。
	/// </summary>
	/// <typeparam name="T">比較する値の型</typeparam>
	/// <param name="searchType">検索タイプ</param>
	/// <param name="propertySelector">MediaFileのプロパティセレクター</param>
	/// <param name="value">比較する定数値</param>
	/// <returns>Expression&lt;Func&lt;MediaFile, bool&gt;&gt;</returns>
	public static Expression<Func<TEntity, bool>> BuildComparisonExpression<TEntity, T>(
		SearchTypeComparison searchType,
		Expression<Func<TEntity, T>> propertySelector,
		T value) {
		var op = GetBinaryExpressionFactory(searchType);
		var param = propertySelector.Parameters[0];
		var left = propertySelector.Body;
		var right = Expression.Constant(value, typeof(T));
		var body = op(left, right);
		return Expression.Lambda<Func<TEntity, bool>>(body, param);
	}
}