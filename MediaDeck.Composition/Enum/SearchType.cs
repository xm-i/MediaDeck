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
	LessThan,

	/// <summary>
	/// 含む
	/// </summary>
	Contains
}

public static class SearchTypeConverters {
	/// <summary>
	/// 検索タイプ毎に適切な比較メソッドを返却する。
	/// </summary>
	/// <typeparam name="T">比較する型</typeparam>
	/// <param name="searchType">検索タイプ</param>
	/// <returns>比較用メソッド</returns>
	public static Func<T, T, bool> SearchTypeToFunc<T>(SearchTypeComparison searchType) {
		var p1 = Expression.Parameter(typeof(T));
		var p2 = Expression.Parameter(typeof(T));
		var body = BuildComparisonBody(searchType, p1, p2);
		var func = Expression.Lambda<Func<T, T, bool>>(body, p1, p2);

		return func.Compile();
	}

	/// <summary>
	/// 検索タイプに対応するBinaryExpression生成関数を返却する。
	/// </summary>
	/// <param name="searchType">検索タイプ</param>
	/// <returns>BinaryExpression生成関数</returns>
	[Obsolete("Use BuildComparisonBody instead to support non-binary operations like Contains.")]
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
	/// 検索タイプに対応する比較Expressionボディを生成する。
	/// </summary>
	public static Expression BuildComparisonBody(SearchTypeComparison searchType, Expression left, Expression right) {
		if (searchType == SearchTypeComparison.Contains && left.Type == typeof(string)) {
			var method = typeof(string).GetMethod("Contains", [typeof(string)])
				?? throw new InvalidOperationException("string.Contains(string) method not found.");
			return Expression.Call(left, method, right);
		}

		return searchType switch {
			SearchTypeComparison.GreaterThan => Expression.GreaterThan(left, right),
			SearchTypeComparison.GreaterThanOrEqual => Expression.GreaterThanOrEqual(left, right),
			SearchTypeComparison.Equal => Expression.Equal(left, right),
			SearchTypeComparison.LessThanOrEqual => Expression.LessThanOrEqual(left, right),
			SearchTypeComparison.LessThan => Expression.LessThan(left, right),
			_ => throw new ArgumentOutOfRangeException(nameof(searchType))
		};
	}

	/// <summary>
	/// 検索タイプに対応する比較Expression（MediaItemのプロパティと定数値の比較）を生成する。
	/// </summary>
	/// <typeparam name="T">比較する値の型</typeparam>
	/// <param name="searchType">検索タイプ</param>
	/// <param name="propertySelector">MediaItemのプロパティセレクター</param>
	/// <param name="value">比較する定数値</param>
	/// <returns>Expression&lt;Func&lt;MediaItem, bool&gt;&gt;</returns>
	public static Expression<Func<TEntity, bool>> BuildComparisonExpression<TEntity, T>(
		SearchTypeComparison searchType,
		Expression<Func<TEntity, T>> propertySelector,
		T value) {
		var param = propertySelector.Parameters[0];
		var left = propertySelector.Body;
		var right = Expression.Constant(value, typeof(T));
		var body = BuildComparisonBody(searchType, left, right);
		return Expression.Lambda<Func<TEntity, bool>>(body, param);
	}
}