using System.Collections.Generic;
using System.ComponentModel;

using MediaDeck.Composition.Bases;
using MediaDeck.FileTypes.Base.Models.Interfaces;
using MediaDeck.Utils.Enums;

namespace MediaDeck.Models.Files.Sort;
/// <summary>
/// ソート条件
/// </summary>
public class SortItem<TKey> : ModelBase, ISortItem{
	/// <summary>
	/// 保存時のキー値
	/// </summary>
	public SortItemKey Key {
		get {
			return this.GetValue<SortItemKey>();
		}
		set {
			this.SetValue(value);
		}
	}

	/// <summary>
	/// ソートの方向
	/// </summary>
	public ListSortDirection Direction {
		get {
			return this.GetValue<ListSortDirection>();
		}
		set {
			this.SetValue(value);
		}
	}

	/// <summary>
	/// ソートキー
	/// </summary>
	public Func<IFileModel, TKey> KeySelector {
		get {
			return this.GetValue<Func<IFileModel, TKey>>()!;
		}
		set {
			this.SetValue(value);
		}
	}

	/// <summary>
	/// コンストラクタ
	/// </summary>
	/// <param name="key">保存時のキー</param>
	/// <param name="direction">ソート方向</param>
	public SortItem(SortItemKey key, Func<IFileModel, TKey> keySelector, ListSortDirection direction = ListSortDirection.Ascending) {
		this.Key = key;
		this.KeySelector = keySelector;
		this.Direction = direction;
	}

	/// <summary>
	/// ソート適用
	/// </summary>
	/// <param name="items">ソートを適用するアイテムリスト</param>
	/// <param name="reverse">ソート方向の反転を行うか否か true:反転する false:反転しない</param>
	/// <returns>整列されたアイテムリスト</returns>
	public IOrderedEnumerable<IFileModel> ApplySort(IEnumerable<IFileModel> items, bool reverse) {
		if (this.Direction == ListSortDirection.Ascending ^ reverse) {
			return items.OrderBy(this.KeySelector);
		} else {
			return items.OrderByDescending(this.KeySelector);
		}
	}

	/// <summary>
	/// ソートされたアイテムリストに対して、追加のソート条件適用
	/// </summary>
	/// <param name="items">ソートを適用するアイテムリスト</param>
	/// <param name="reverse">ソート方向の反転を行うか否か true:反転する false:反転しない</param>
	/// <returns>整列されたアイテムリスト</returns>
	public IOrderedEnumerable<IFileModel> ApplyThenBySort(IOrderedEnumerable<IFileModel> items, bool reverse) {
		if (this.Direction == ListSortDirection.Ascending ^ reverse) {
			return items.ThenBy(this.KeySelector);
		} else {
			return items.ThenByDescending(this.KeySelector);
		}
	}

	public override string ToString() {
		return $"<[{base.ToString()}] {this.Key}>";
	}
}

public interface ISortItem {
	/// <summary>
	/// 保存時のキー値
	/// </summary>
	SortItemKey Key {
		get;
		set;
	}

	/// <summary>
	/// ソートの方向
	/// </summary>
	ListSortDirection Direction {
		get;
		set;
	}

	/// <summary>
	/// ソート適用
	/// </summary>
	/// <param name="items">ソートを適用するアイテムリスト</param>
	/// <returns>整列されたアイテムリスト</returns>
	IOrderedEnumerable<IFileModel> ApplySort(IEnumerable<IFileModel> items, bool reverse);

	/// <summary>
	/// ソートされたアイテムリストに対して、追加のソート条件適用
	/// </summary>
	/// <param name="items">ソートを適用するアイテムリスト</param>
	/// <returns>整列されたアイテムリスト</returns>
	IOrderedEnumerable<IFileModel> ApplyThenBySort(IOrderedEnumerable<IFileModel> items, bool reverse);
}