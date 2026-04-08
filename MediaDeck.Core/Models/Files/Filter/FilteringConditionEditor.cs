using MediaDeck.Common.Base;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Composition.Objects;
using MediaDeck.Composition.Stores.State.Model.Objects;
using MediaDeck.Core.Models.Files.Filter.FilterItemObjects;

namespace MediaDeck.Core.Models.Files.Filter;

/// <summary>
/// フィルタリング条件
/// </summary>
/// <remarks>
/// Add***Filterメソッドでフィルタークリエイターを<see cref="FilterItemObjects"/>に追加し、
/// <see cref="RemoveFilter(IFilterItemObject)"/>メソッドで削除する。
/// </remarks>
public class FilteringConditionEditor : ModelBase {
	/// <param name="filterObject">復元用フィルターオブジェクト</param>
	public FilteringConditionEditor(FilterObject filterObject) {
		this.FilterObject = filterObject;
		this.DisplayName = filterObject.DisplayName;
		this.FilterItemObjects = this.FilterObject.FilterItemObjects;
	}

	/// <summary>
	/// 表示名
	/// </summary>
	public ReactiveProperty<string> DisplayName {
		get;
	}

	/// <summary>
	/// フィルター条件クリエイター
	/// </summary>
	public ObservableList<IFilterItemObject> FilterItemObjects {
		get;
	}

	/// <summary>
	/// フィルター保存用オブジェクト
	/// </summary>
	public FilterObject FilterObject {
		get;
	}

	/// <summary>
	/// フィルター追加
	/// </summary>
	/// <param name="filterItemObject">追加するフィルター</param>
	public void AddFilter(IFilterItemObject filterItemObject) {
		this.FilterItemObjects.Add(filterItemObject);
	}

	/// <summary>
	/// タグフィルター追加
	/// </summary>
	/// <param name="tagName">タグ名</param>
	/// <param name="searchType">検索タイプ</param>
	public void AddTagFilter(string tagName, SearchTypeInclude searchType) {
		this.FilterItemObjects.Add(new TagFilterItemObject { TagName = tagName, SearchType = searchType });
	}

	/// <summary>
	/// ファイルパスフィルター追加
	/// </summary>
	/// <param name="text">ファイルパスに含まれる文字列</param>
	/// <param name="searchType">検索タイプ</param>
	public void AddFilePathFilter(string text, SearchTypeInclude searchType) {
		this.FilterItemObjects.Add(new FilePathFilterItemObject { Text = text, SearchType = searchType });
	}

	/// <summary>
	/// 評価フィルター追加
	/// </summary>
	/// <param name="rate">評価</param>
	/// <param name="searchType">検索タイプ</param>
	public void AddRateFilter(int rate, SearchTypeComparison searchType) {
		this.FilterItemObjects.Add(new RateFilterItemObject { Rate = rate, SearchType = searchType });
	}

	/// <summary>
	/// 解像度フィルター追加
	/// </summary>
	/// <param name="width">幅</param>
	/// <param name="height">高さ</param>
	/// <param name="searchType">検索タイプ</param>
	public void AddResolutionFilter(int? width, int? height, SearchTypeComparison searchType) {
		IFilterItemObject filterItemObject;
		if (width is { } w && height is { } h) {
			filterItemObject = new ResolutionFilterItemObject { Resolution = new ComparableSize(w, h), SearchType = searchType };
		} else {
			filterItemObject = new ResolutionFilterItemObject { Width = width, Height = height, SearchType = searchType };
		}
		this.FilterItemObjects.Add(filterItemObject);
	}

	/// <summary>
	/// メディアタイプフィルター追加
	/// </summary>
	/// <param name="isVideo">動画か否か</param>
	public void AddMediaTypeFilter(bool isVideo) {
		this.FilterItemObjects.Add(new MediaTypeFilterItemObject { IsVideo = isVideo });
	}

	/// <summary>
	/// 座標フィルター追加
	/// </summary>
	/// <param name="hasLocation">座標情報を含むか否か</param>
	public void AddLocationFilter(bool hasLocation) {
		this.FilterItemObjects.Add(new LocationFilterItemObject { Contains = hasLocation });
	}

	/// <summary>
	///ファイル存在フィルター追加
	/// </summary>
	/// <param name="exists">ファイルが存在するか否か</param>
	public void AddExistsFilter(bool exists) {
		this.FilterItemObjects.Add(new ExistsFilterItemObject { Exists = exists });
	}

	/// <summary>
	/// フィルター削除
	/// </summary>
	/// <param name="filterItemObject">削除対象フィルタークリエイター</param>
	public void RemoveFilter(IFilterItemObject filterItemObject) {
		this.FilterItemObjects.Remove(filterItemObject);
	}
}