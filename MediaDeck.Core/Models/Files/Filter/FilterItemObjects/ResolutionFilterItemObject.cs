using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Composition.Objects;
using R3.JsonConfig.Attributes;

namespace MediaDeck.Core.Models.Files.Filter.FilterItemObjects;

/// <summary>
/// 解像度フィルターアイテムオブジェクト
/// </summary>
[GenerateR3JsonConfigDto]
[JsonConfigDerivedType("resolution")]
[Inject(InjectServiceLifetime.Transient)]
public class ResolutionFilterItemObject : IFilterItemObject {
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
			if (this.Width != null) {
				return $"Width is {this.Width} {com}";
			}
			if (this.Height != null) {
				return $"Height is {this.Height} {com}";
			}
			if (this.Resolution != null) {
				return $"Resolution is {this.Resolution} {com}";
			}
			throw new InvalidOperationException();
		}
	}

	/// <summary>
	/// 幅
	/// </summary>
	public int? Width {
		get;
		set;
	}

	/// <summary>
	/// 高さ
	/// </summary>
	public int? Height {
		get;
		set;
	}

	/// <summary>
	/// 解像度
	/// </summary>
	public ComparableSize? Resolution {
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

	public ResolutionFilterItemObject() {
	}
}