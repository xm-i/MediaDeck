using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.Files;
using R3.JsonConfig.Attributes;

namespace MediaDeck.Core.Models.Files.Filter.FilterItemObjects;

/// <summary>
/// 評価フィルターアイテムオブジェクト
/// </summary>
[GenerateR3JsonConfigDto]
[JsonConfigDerivedType("rate")]
[Inject(InjectServiceLifetime.Transient)]
public class RateFilterItemObject : IFilterItemObject {
	/// <summary>
	/// 表示名
	/// </summary>
	public string DisplayName {
		get {
			var com = new Dictionary<SearchTypeComparison, string> {
				{ SearchTypeComparison.GreaterThan, "greater than" },
				{ SearchTypeComparison.GreaterThanOrEqual, "greater than or equal to" },
				{ SearchTypeComparison.Equal, "equal to" },
				{ SearchTypeComparison.LessThanOrEqual, "less than or equal to" },
				{ SearchTypeComparison.LessThan, "less than" }
			}[this.SearchType];
			return $"Rating is {this.Rate} {com}";
		}
	}

	/// <summary>
	/// 評価
	/// </summary>
	public int Rate {
		get;
		set;
	}

	/// <summary>
	/// 検索タイプ
	/// </summary>
	public SearchTypeComparison SearchType {
		get;
		set;
	}

	public RateFilterItemObject() {
	}
}