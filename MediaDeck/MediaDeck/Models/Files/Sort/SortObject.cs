using System.Collections.Generic;

namespace MediaDeck.Models.Files.Sort;
/// <summary>
/// フィルター設定復元用オブジェクト
/// </summary>
public class SortObject {
		/// <summary>
		/// 表示名
		/// </summary>
		public ReactiveProperty<string> DisplayName {
			get;
			set;
		} = new ();

		/// <summary>
		/// フィルター条件クリエイター
		/// </summary>
		public ObservableList<SortItemCreator> SortItemCreators {
			get;
			set;
		} = [];

		public SortObject() {
		}

		public SortObject(string displayName, IEnumerable<SortItemCreator> sortItemCreators) {
			this.DisplayName.Value = displayName;
			this.SortItemCreators.AddRange(sortItemCreators);
		}

		public bool Equals(SortObject? other) {
			if (other is null) {
				return false;
			}

			if (ReferenceEquals(this, other)) {
				return true;
			}

			return Equals(this.DisplayName.Value, other.DisplayName.Value) && this.SortItemCreators.SequenceEqual(other.SortItemCreators);
		}

		public override bool Equals(object? obj) {
			return obj is SortObject rso && this.Equals(rso);
		}

		public override int GetHashCode() {
			unchecked {
				return ((this.DisplayName.Value?.GetHashCode() ?? 0) * 397) ^ this.SortItemCreators.Select(x => x.GetHashCode()).Aggregate((x, y) => x ^ y);
			}
		}
	}
