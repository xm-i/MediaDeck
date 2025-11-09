using System.Collections.Generic;

using MediaDeck.Utils.Enums;
using MediaDeck.Utils.Objects;

namespace MediaDeck.Models.Files.Filter.FilterItemObjects;
/// <summary>
/// 解像度フィルターアイテムオブジェクト
/// </summary>
public class ResolutionFilterItemObject : IFilterItemObject {
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

	[Obsolete("for serialize")]
	public ResolutionFilterItemObject() {
	}

	/// <summary>
	/// コンストラクタ
	/// </summary>
	/// <param name="resolution">解像度</param>
	/// <param name="searchType">検索タイプ</param>
	public ResolutionFilterItemObject(ComparableSize resolution, SearchTypeComparison searchType) {
		if (!Enum.IsDefined(typeof(SearchTypeComparison), searchType)) {
			throw new ArgumentException();
		}
		this.Resolution = resolution;
		this.SearchType = searchType;
	}

	/// <summary>
	/// コンストラクタ
	/// </summary>
	/// <param name="width">幅</param>
	/// <param name="height">高さ</param>
	/// <param name="searchType">検索タイプ</param>
	public ResolutionFilterItemObject(int? width, int? height, SearchTypeComparison searchType) {
		if (!(width == null ^ height == null) || !Enum.IsDefined(typeof(SearchTypeComparison), searchType)) {
			throw new ArgumentException();
		}
		this.Width = width;
		this.Height = height;
		this.SearchType = searchType;
	}
}
