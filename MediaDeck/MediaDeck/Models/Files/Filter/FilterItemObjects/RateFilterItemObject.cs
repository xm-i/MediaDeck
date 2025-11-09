using System.Collections.Generic;

using MediaDeck.Utils.Enums;

namespace MediaDeck.Models.Files.Filter.FilterItemObjects;
/// <summary>
/// 評価フィルターアイテムオブジェクト
/// </summary>
public class RateFilterItemObject : IFilterItemObject {
	/// <summary>
	/// 表示名
	/// </summary>
	public string DisplayName {
		get {
			var com = new Dictionary<SearchTypeComparison, string> {
				{SearchTypeComparison.GreaterThan, "greater than"},
				{SearchTypeComparison.GreaterThanOrEqual, "greater than or equal to"},
				{SearchTypeComparison.Equal, "equal to"},
				{SearchTypeComparison.LessThanOrEqual, "less than or equal to"},
				{SearchTypeComparison.LessThan, "less than"}
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

	[Obsolete("for serialize")]
	public RateFilterItemObject() {
	}

	/// <summary>
	/// コンストラクタ
	/// </summary>
	/// <param name="rate">評価</param>
	/// <param name="searchType">検索タイプ</param>
	public RateFilterItemObject(int rate, SearchTypeComparison searchType) {
		if (!Enum.IsDefined(typeof(SearchTypeComparison), searchType)) {
			throw new ArgumentException();
		}
		this.Rate = rate;
		this.SearchType = searchType;
	}
}
