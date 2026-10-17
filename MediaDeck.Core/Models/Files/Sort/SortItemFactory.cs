using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Stores.State.Model.Objects;

namespace MediaDeck.Core.Models.Files.Sort;

/// <summary>
/// ソート項目の生成を行うファクトリクラス
/// </summary>
public static class SortItemFactory {
	/// <summary>
	/// ソート項目オブジェクトからソート項目インスタンスを生成します。
	/// </summary>
	/// <param name="sortItemObject">ソート項目オブジェクト</param>
	/// <returns>生成されたソート項目インスタンス</returns>
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
			SortItemKey.Duration => new SortItem(sortItemObject.SortItemKey, mf => (object?)(mf.VideoFile != null ? mf.VideoFile.Duration : null), sortItemObject.Direction),
			_ => throw new ArgumentException(),
		};
	}
}