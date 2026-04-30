using System.Linq.Expressions;

using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Composition.Tables;

using R3.JsonConfig.Attributes;

namespace MediaDeck.Core.Models.Files.SearchConditions;

/// <summary>
/// <see cref="MediaItem"/> の任意プロパティに対する比較条件を表す検索条件。
/// 検索入力欄で <c>prop.&lt;PropertyName&gt;</c> のプレフィックス入力時にサジェストされ、
/// 演算子と値が確定した後に <see cref="WherePredicate"/> が有効な式ツリーを返す。
/// </summary>
[GenerateR3JsonConfigDto]
[JsonConfigDerivedType("property")]
[Inject(InjectServiceLifetime.Transient)]
public class PropertySearchCondition : ISearchCondition {
	public PropertySearchCondition() {
	}

	/// <summary>対象プロパティ名（<see cref="MediaItem"/> のメンバ名）</summary>
	public string PropertyName {
		get {
			return field ?? throw new InvalidOperationException($"{nameof(this.PropertyName)} is not initialized.");
		}
		set {
			field = value;
		}
	}

	/// <summary>比較演算子</summary>
	public SearchTypeComparison Operator {
		get;
		set;
	} = SearchTypeComparison.Equal;

	/// <summary>比較値（文字列リテラル。プロパティ型に応じてパース）</summary>
	public string Value {
		get;
		set;
	} = string.Empty;

	/// <summary>
	/// <c>true</c> の場合、サジェスト用のスタブではなくユーザー入力済みの実条件として扱う。
	/// </summary>
	public bool IsConfigured {
		get;
		set;
	}

	public string DisplayText {
		get {
			if (!this.IsConfigured) {
				return $"prop.{this.PropertyName}";
			}
			return $"prop.{this.PropertyName} {OperatorToSymbol(this.Operator)} {this.Value}";
		}
	}

	public Expression<Func<MediaItem, bool>>? WherePredicate {
		get {
			if (!this.IsConfigured) {
				return null;
			}
			var descriptor = MediaItemPropertyCatalog.Find(this.PropertyName);
			return descriptor?.Build(this.Operator, this.Value);
		}
	}

	public bool IsMatchForSuggest(string searchWord) {
		return $"prop.{this.PropertyName}".StartsWith(searchWord, StringComparison.OrdinalIgnoreCase);
	}

	private static string OperatorToSymbol(SearchTypeComparison op) {
		return op switch {
			SearchTypeComparison.GreaterThan => ">",
			SearchTypeComparison.GreaterThanOrEqual => ">=",
			SearchTypeComparison.Equal => "=",
			SearchTypeComparison.LessThanOrEqual => "<=",
			SearchTypeComparison.LessThan => "<",
			_ => "?",
		};
	}
}