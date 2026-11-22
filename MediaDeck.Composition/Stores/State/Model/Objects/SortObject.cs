using System.ComponentModel;
using GenJsonConfig.Attributes;
using MediaDeck.Composition.Enum;
using Microsoft.Extensions.DependencyInjection;

namespace MediaDeck.Composition.Stores.State.Model.Objects;

/// <summary>
/// ソート設定復元用オブジェクト
/// </summary>
[Inject(InjectServiceLifetime.Transient)]
[GenerateJsonConfigDto]
public class SortObject(IServiceProvider serviceProvider) {
	/// <summary>
	/// ID
	/// </summary>
	public Guid Id {
		get;
		set;
	} = Guid.NewGuid();

	/// <summary>
	/// 表示名
	/// </summary>
	public ReactiveProperty<string> DisplayName {
		get;
	} = new();

	/// <summary>
	/// ソート条件クリエイター
	/// </summary>
	public ObservableList<SortItemObject> SortItemObjects {
		get;
	} = [];

	/// <summary>
	/// 空のソート項目を追加します。
	/// </summary>
	/// <returns>追加されたソート項目オブジェクト。</returns>
	public SortItemObject AddSortItemObject() {
		var sio = serviceProvider.GetRequiredService<SortItemObject>();
		this.SortItemObjects.Add(sio);
		return sio;
	}

	/// <summary>
	/// 指定したソートキーと方向でソート項目を追加します。
	/// </summary>
	/// <param name="sortItemKey">ソートキー。</param>
	/// <param name="direction">ソート方向。</param>
	/// <returns>追加されたソート項目オブジェクト。</returns>
	public SortItemObject AddSortItemObject(SortItemKey sortItemKey, ListSortDirection direction) {
		var sio = serviceProvider.GetRequiredService<SortItemObject>();
		sio.SortItemKey = sortItemKey;
		sio.Direction = direction;
		this.SortItemObjects.Add(sio);
		return sio;
	}

	/// <summary>
	/// 指定した位置のソート項目を新しい位置へ移動します。
	/// </summary>
	/// <param name="oldIndex">移動元のインデックス。</param>
	/// <param name="newIndex">移動先のインデックス。</param>
	public void MoveSortItemObject(int oldIndex, int newIndex) {
		if (oldIndex == newIndex ||
			oldIndex < 0 || oldIndex >= this.SortItemObjects.Count ||
			newIndex < 0 || newIndex >= this.SortItemObjects.Count) {
			return;
		}
		this.SortItemObjects.Move(oldIndex, newIndex);
	}

	/// <summary>
	/// 指定したインデックスのソート項目を新しいキーと方向のオブジェクトで置換します。
	/// </summary>
	/// <param name="index">置換対象のインデックス。</param>
	/// <param name="sortItemKey">新しいソートキー。</param>
	/// <param name="direction">新しいソート方向。</param>
	/// <returns>置換後のソート項目オブジェクト。範囲外の場合は <see langword="null"/>。</returns>
	public SortItemObject? ReplaceSortItemObject(int index, SortItemKey sortItemKey, ListSortDirection direction) {
		if (index < 0 || index >= this.SortItemObjects.Count) {
			return null;
		}
		var sio = serviceProvider.GetRequiredService<SortItemObject>();
		sio.SortItemKey = sortItemKey;
		sio.Direction = direction;
		this.SortItemObjects[index] = sio;
		return sio;
	}

	/// <summary>
	/// 指定したソート項目を削除します。
	/// </summary>
	/// <param name="sortItemObject">削除対象のソート項目オブジェクト。</param>
	public void RemoveSortItemObject(SortItemObject sortItemObject) {
		this.SortItemObjects.Remove(sortItemObject);
	}

	/// <summary>
	/// 別の <see cref="SortObject"/> と等価であるかを判定します。
	/// </summary>
	/// <param name="other">比較対象のオブジェクト。</param>
	/// <returns>等価である場合は <see langword="true"/>。</returns>
	public bool Equals(SortObject? other) {
		if (other is null) {
			return false;
		}

		if (ReferenceEquals(this, other)) {
			return true;
		}

		return Equals(this.DisplayName.Value, other.DisplayName.Value) && this.SortItemObjects.SequenceEqual(other.SortItemObjects);
	}

	/// <inheritdoc />
	public override bool Equals(object? obj) {
		return obj is SortObject rso && this.Equals(rso);
	}

	/// <inheritdoc />
	public override int GetHashCode() {
		unchecked {
			return ((this.DisplayName.Value?.GetHashCode() ?? 0) * 397) ^ this.SortItemObjects.Select(x => x.GetHashCode()).Aggregate(0, (x, y) => x ^ y);
		}
	}
}