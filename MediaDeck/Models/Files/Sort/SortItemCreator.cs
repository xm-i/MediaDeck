using System.ComponentModel;

using MediaDeck.Utils.Enums;
using MediaDeck.Utils.Objects;

namespace MediaDeck.Models.Files.Sort;

public class SortItemCreator {

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

		[Obsolete("for serialize")]
		public SortItemCreator() {

		}

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="sortItemKey">ソートキー</param>
		/// <param name="direction">ソート方向</param>
		public SortItemCreator(SortItemKey sortItemKey, ListSortDirection direction = ListSortDirection.Ascending) {
			this.SortItemKey = sortItemKey;
			this.Direction = direction;
		}

		public ISortItem Create() {
			return this.SortItemKey switch {
				SortItemKey.FilePath => new SortItem<string>(this.SortItemKey, x => x.FilePath, this.Direction),
				SortItemKey.CreationTime => new SortItem<DateTime>(this.SortItemKey, x => x.CreationTime, this.Direction),
				SortItemKey.ModifiedTime => new SortItem<DateTime>(this.SortItemKey, x => x.ModifiedTime, this.Direction),
				SortItemKey.LastAccessTime => new SortItem<DateTime>(this.SortItemKey, x => x.LastAccessTime, this.Direction),
				SortItemKey.RegisteredTime => new SortItem<DateTime>(this.SortItemKey, x => x.RegisteredTime, this.Direction),
				SortItemKey.FileSize => new SortItem<long>(this.SortItemKey, x => x.FileSize, this.Direction),
				SortItemKey.Rate => new SortItem<double>(this.SortItemKey, x => x.Rate, this.Direction),
				SortItemKey.Location => new SortItem<GpsLocation?>(this.SortItemKey, x => x.Location, this.Direction),
				SortItemKey.Resolution => new SortItem<ComparableSize?>(this.SortItemKey, x => x.Resolution, this.Direction),
				SortItemKey.UsageCount => new SortItem<int>(this.SortItemKey, x => x.UsageCount, this.Direction),
				_ => throw new ArgumentException(),
			};
		}

		public bool Equals(SortItemCreator? other) {
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

			return obj is SortItemCreator sic && this.Equals(sic);
		}

		public override int GetHashCode() {
			unchecked {
				return ((int)this.SortItemKey * 397) ^ (int)this.Direction;
			}
		}
	}
