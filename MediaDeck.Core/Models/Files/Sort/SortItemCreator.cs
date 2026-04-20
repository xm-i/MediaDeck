using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Stores.State.Model.Objects;

namespace MediaDeck.Core.Models.Files.Sort;

public static class SortItemFactory {
	public static ISortItem Create(SortItemObject sortItemObject) {
		return sortItemObject.SortItemKey switch {
			SortItemKey.FilePath => new SortItem(sortItemObject.SortItemKey, mf => (object?)mf.FilePath, sortItemObject.Direction),
			SortItemKey.CreationTime => new SortItem(sortItemObject.SortItemKey, mf => (object?)mf.CreationTime, sortItemObject.Direction),
			SortItemKey.ModifiedTime => new SortItem(sortItemObject.SortItemKey, mf => (object?)mf.ModifiedTime, sortItemObject.Direction),
			SortItemKey.LastAccessTime => new SortItem(sortItemObject.SortItemKey, mf => (object?)mf.LastAccessTime, sortItemObject.Direction),
			SortItemKey.RegisteredTime => new SortItem(sortItemObject.SortItemKey, mf => (object?)mf.RegisteredTime, sortItemObject.Direction),
			SortItemKey.FileSize => new SortItem(sortItemObject.SortItemKey, mf => (object?)mf.FileSize, sortItemObject.Direction),
			SortItemKey.Rate => new SortItem(sortItemObject.SortItemKey, mf => (object?)mf.Rate, sortItemObject.Direction),
			SortItemKey.Location => new SortItem(sortItemObject.SortItemKey, mf => (object?)mf.Latitude, sortItemObject.Direction),
			SortItemKey.Resolution => new SortItem(sortItemObject.SortItemKey, mf => (object?)((long)mf.Width * mf.Height), sortItemObject.Direction),
			SortItemKey.UsageCount => new SortItem(sortItemObject.SortItemKey, mf => (object?)mf.UsageCount, sortItemObject.Direction),
			_ => throw new ArgumentException(),
		};
	}
}