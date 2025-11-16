using System.ComponentModel;

using MediaDeck.Composition.Enum;

using R3.JsonConfig.Attributes;

namespace MediaDeck.Composition.Stores.State.Model.Objects;

[AddScoped]
[GenerateR3JsonConfigDefaultDto]
public class SortItemObject {

	/// <summary>
	/// ソートキー
	/// </summary>
	public SortItemKey SortItemKey {
		get;
		set;
	}

	/// <summary>
	/// ソート方向
	/// </summary>
	public ListSortDirection Direction {
		get;
		set;
	}
	public bool Equals(SortItemObject? other) {
		if (other is null) {
			return false;
		}

		if (ReferenceEquals(this, other)) {
			return true;
		}

		return this.SortItemKey == other.SortItemKey && this.Direction == other.Direction;
	}

	public override bool Equals(object? obj) {
		if (obj is null) {
			return false;
		}

		if (ReferenceEquals(this, obj)) {
			return true;
		}

		return obj is SortItemObject sic && this.Equals(sic);
	}

	public override int GetHashCode() {
		unchecked {
			return ((int)this.SortItemKey * 397) ^ (int)this.Direction;
		}
	}
}
