using System.ComponentModel;

using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Objects;
using MediaDeck.Composition.Stores.State.Model.Objects;

namespace MediaDeck.Models.Files.Sort;

public static class SortItemFactory {
	public static ISortItem Create(SortItemObject sortItemObject) {
		return sortItemObject.SortItemKey switch {
			SortItemKey.FilePath => new SortItem<string>(sortItemObject.SortItemKey, x => x.FilePath, sortItemObject.Direction),
			SortItemKey.CreationTime => new SortItem<DateTime>(sortItemObject.SortItemKey, x => x.CreationTime, sortItemObject.Direction),
			SortItemKey.ModifiedTime => new SortItem<DateTime>(sortItemObject.SortItemKey, x => x.ModifiedTime, sortItemObject.Direction),
			SortItemKey.LastAccessTime => new SortItem<DateTime>(sortItemObject.SortItemKey, x => x.LastAccessTime, sortItemObject.Direction),
			SortItemKey.RegisteredTime => new SortItem<DateTime>(sortItemObject.SortItemKey, x => x.RegisteredTime, sortItemObject.Direction),
			SortItemKey.FileSize => new SortItem<long>(sortItemObject.SortItemKey, x => x.FileSize, sortItemObject.Direction),
			SortItemKey.Rate => new SortItem<double>(sortItemObject.SortItemKey, x => x.Rate, sortItemObject.Direction),
			SortItemKey.Location => new SortItem<GpsLocation?>(sortItemObject.SortItemKey, x => x.Location, sortItemObject.Direction),
			SortItemKey.Resolution => new SortItem<ComparableSize?>(sortItemObject.SortItemKey, x => x.Resolution, sortItemObject.Direction),
			SortItemKey.UsageCount => new SortItem<int>(sortItemObject.SortItemKey, x => x.UsageCount, sortItemObject	.Direction),
			_ => throw new ArgumentException(),
		};
	}
}
