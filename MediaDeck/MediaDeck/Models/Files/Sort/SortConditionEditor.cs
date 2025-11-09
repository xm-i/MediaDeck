using System.Collections.Generic;

using MediaDeck.Composition.Bases;

namespace MediaDeck.Models.Files.Sort;
/// <summary>
/// フィルタリング条件
/// </summary>
/// <remarks>
/// AddSortメソッドでソートクリエイターを<see cref="SortItemCreators"/>に追加し、
/// <see cref="RemoveSortItem(SortItemCreator)"/>メソッドで削除する。
/// </remarks>
public class SortConditionEditor : ModelBase {
	/// <param name="sortObject">復元用ソートオブジェクト</param>
	public SortConditionEditor(SortObject sortObject) {
		this.SortObject = sortObject;
		this.Load();
	}

	/// <summary>
	/// 表示名
	/// </summary>
	public ReactiveProperty<string> DisplayName {
		get;
	} = new();

	/// <summary>
	/// ソート条件クリエイター
	/// </summary>
	public ObservableList<SortItemCreator> SortItemCreators {
		get;
	} = [];

	/// <summary>
	/// ソート保存用オブジェクト
	/// </summary>
	public SortObject SortObject {
		get;
	}

	public void Load() {
		this.DisplayName.Value = this.SortObject.DisplayName.Value;
		this.SortItemCreators.Clear();
		this.SortItemCreators.AddRange(this.SortObject.SortItemCreators);
	}

	/// <summary>
	/// 復元用フィルターオブジェクトを更新する。
	/// </summary>
	public void Save() {
		this.SortObject.DisplayName.Value = this.DisplayName.Value;
		this.SortObject.SortItemCreators.Clear();
		this.SortObject.SortItemCreators.AddRange(this.SortItemCreators);
	}

	/// <summary>
	/// ソートアイテムの追加
	/// </summary>
	/// <param name="sortItem"></param>
	public void AddSortItem(SortItemCreator sortItem) {
		this.SortObject.SortItemCreators.Add(sortItem);
		this.Load();
	}

	/// <summary>
	/// ソートアイテムの削除
	/// </summary>
	/// <param name="sortItem"></param>
	public void RemoveSortItem(SortItemCreator sortItem) {
		this.SortObject.SortItemCreators.Remove(sortItem);
		this.Load();
	}
}
