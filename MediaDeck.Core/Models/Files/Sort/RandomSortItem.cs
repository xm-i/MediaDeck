using System.ComponentModel;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Tables;

namespace MediaDeck.Core.Models.Files.Sort;

/// <summary>
/// シード値に基づいた決定論的ランダムソート条件
/// </summary>
public class RandomSortItem : ISortItem {
	/// <summary>
	/// 保存時のキー値
	/// </summary>
	public SortItemKey Key {
		get;
		set;
	} = SortItemKey.Random;

	/// <summary>
	/// ソートの方向（ランダムソートでは動作上無視されます）
	/// </summary>
	public ListSortDirection Direction {
		get;
		set;
	}

	/// <summary>
	/// 乱数シード値
	/// </summary>
	public int Seed {
		get;
	}

	/// <summary>
	/// <see cref="RandomSortItem"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="seed">乱数シード値</param>
	/// <param name="key">保存時のキー</param>
	/// <param name="direction">ソート方向</param>
	public RandomSortItem(int seed = 1, SortItemKey key = SortItemKey.Random, ListSortDirection direction = ListSortDirection.Ascending) {
		this.Key = key;
		this.Direction = direction;
		this.Seed = seed == 0 ? 1 : seed;
	}

	/// <summary>
	/// IQueryable用ソート適用（シード値に基づく二乗剰余ハッシュ式）
	/// </summary>
	public IOrderedQueryable<MediaItem> ApplySort(IQueryable<MediaItem> query, bool reverse) {
		var (a, seedVal, modInner, prime) = this.GetHashParameters();
		return query.OrderBy(m => ((m.MediaItemId * a) + seedVal) % modInner * (((m.MediaItemId * a) + seedVal) % modInner) % prime);
	}

	/// <summary>
	/// IQueryable用追加ソート適用（シード値に基づく二乗剰余ハッシュ式）
	/// </summary>
	public IOrderedQueryable<MediaItem> ApplyThenBySort(IOrderedQueryable<MediaItem> query, bool reverse) {
		var (a, seedVal, modInner, prime) = this.GetHashParameters();
		return query.ThenBy(m => ((m.MediaItemId * a) + seedVal) % modInner * (((m.MediaItemId * a) + seedVal) % modInner) % prime);
	}

	/// <summary>
	/// ハッシュ計算用パラメータを取得します。
	/// </summary>
	private (long a, long seedVal, long modInner, long prime) GetHashParameters() {
		const long prime = 2147483647L;
		const long modInner = 2000000000L;
		var seedVal = Math.Abs((long)this.Seed);
		if (seedVal == 0) {
			seedVal = 1;
		}

		var a = ((seedVal * 1103515245L) + 12345L) % 1000000007L;
		if (a == 0) {
			a = 1;
		}

		return (a, seedVal, modInner, prime);
	}

	public override string ToString() {
		return $"<[{base.ToString()}] {this.Key} (Seed={this.Seed})>";
	}
}