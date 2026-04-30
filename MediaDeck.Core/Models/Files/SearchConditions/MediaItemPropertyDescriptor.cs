using System.Linq.Expressions;

using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Tables;

namespace MediaDeck.Core.Models.Files.SearchConditions;

/// <summary>
/// <see cref="MediaItem"/> のプロパティ記述子。プロパティ毎にサポートする比較演算子と、
/// 文字列リテラルから <see cref="Expression{TDelegate}"/> を組み立てる関数を保持する。
/// </summary>
public sealed class MediaItemPropertyDescriptor {
	private readonly Func<SearchTypeComparison, string, Expression<Func<MediaItem, bool>>?> _build;

	public MediaItemPropertyDescriptor(
		string name,
		Type valueType,
		IReadOnlyList<SearchTypeComparison> supportedOperators,
		Func<SearchTypeComparison, string, Expression<Func<MediaItem, bool>>?> build) {
		this.Name = name;
		this.ValueType = valueType;
		this.SupportedOperators = supportedOperators;
		this._build = build;
	}

	/// <summary>プロパティ名（<see cref="MediaItem"/>のメンバ名と一致）</summary>
	public string Name {
		get;
	}

	/// <summary>値の CLR 型</summary>
	public Type ValueType {
		get;
	}

	/// <summary>このプロパティで使用可能な比較演算子</summary>
	public IReadOnlyList<SearchTypeComparison> SupportedOperators {
		get;
	}

	/// <summary>
	/// 比較演算子と文字列リテラルから <c>WherePredicate</c> 用の式ツリーを生成する。
	/// パースに失敗した場合は <c>null</c> を返す。
	/// </summary>
	public Expression<Func<MediaItem, bool>>? Build(SearchTypeComparison op, string rawValue) {
		return this._build(op, rawValue);
	}
}