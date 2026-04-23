using System.ComponentModel;
using System.Linq.Expressions;

using MediaDeck.Composition.Enum;
using MediaDeck.Database.Tables;

namespace MediaDeck.Core.Models.Files.Sort;

/// <summary>
/// ソート条件
/// </summary>
public class SortItem : ISortItem {
	/// <summary>
	/// 保存時のキー値
	/// </summary>
	public SortItemKey Key {
		get;
		set;
	}

	/// <summary>
	/// ソートの方向
	/// </summary>
	public ListSortDirection Direction {
		get;
		set;
	}

	/// <summary>
	/// ソートキー（Expression Tree）
	/// </summary>
	public Expression<Func<MediaFile, object?>> KeySelector {
		get;
	}

	/// <summary>
	/// コンストラクタ
	/// </summary>
	/// <param name="key">保存時のキー</param>
	/// <param name="dbKeySelector">DB用ソートキー</param>
	/// <param name="direction">ソート方向</param>
	public SortItem(SortItemKey key, Expression<Func<MediaFile, object?>> dbKeySelector, ListSortDirection direction = ListSortDirection.Ascending) {
		this.Key = key;
		this.KeySelector = dbKeySelector;
		this.Direction = direction;
	}

	/// <summary>
	/// IQueryable用ソート適用
	/// </summary>
	public IOrderedQueryable<MediaFile> ApplySort(IQueryable<MediaFile> query, bool reverse) {
		if (this.Direction == ListSortDirection.Ascending ^ reverse) {
			return query.OrderBy(this.KeySelector);
		} else {
			return query.OrderByDescending(this.KeySelector);
		}
	}

	/// <summary>
	/// IQueryable用追加ソート適用
	/// </summary>
	public IOrderedQueryable<MediaFile> ApplyThenBySort(IOrderedQueryable<MediaFile> query, bool reverse) {
		if (this.Direction == ListSortDirection.Ascending ^ reverse) {
			return query.ThenBy(this.KeySelector);
		} else {
			return query.ThenByDescending(this.KeySelector);
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
	public SortItemKey Key {
		get;
		set;
	}

	/// <summary>
	/// ソートの方向
	/// </summary>
	public ListSortDirection Direction {
		get;
		set;
	}

	/// <summary>
	/// IQueryable用ソート適用
	/// </summary>
	public IOrderedQueryable<MediaFile> ApplySort(IQueryable<MediaFile> query, bool reverse);

	/// <summary>
	/// IQueryable用追加ソート適用
	/// </summary>
	public IOrderedQueryable<MediaFile> ApplyThenBySort(IOrderedQueryable<MediaFile> query, bool reverse);
}