using System.ComponentModel;

using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Composition.Stores.State.Model.Objects;

namespace MediaDeck.Core.Models.Files.Sort;

/// <summary>
/// ソートキーおよびソート方向の表示文字列・アイコン・要約テキストを生成するヘルパークラス。
/// </summary>
public static class SortDisplayHelper {
	/// <summary>
	/// 指定されたソートキーに対応する多言語表示名を取得します。
	/// </summary>
	/// <param name="key">ソートキー。</param>
	/// <param name="stringProvider">多言語リソースプロバイダー。</param>
	/// <returns>ソートキーの表示名。</returns>
	public static string GetKeyDisplayName(SortItemKey key, IStringProvider stringProvider) {
		return stringProvider.GetString($"SortItemKey_{key}");
	}

	/// <summary>
	/// 指定されたソートキーに対応する Segoe Fluent Icons グリフを取得します。
	/// </summary>
	/// <param name="key">ソートキー。</param>
	/// <returns>アイコングリフ文字列。</returns>
	public static string GetKeyIconGlyph(SortItemKey key) {
		return key switch {
			SortItemKey.FilePath => "\uE8A5",
			SortItemKey.CreationTime => "\uE787",
			SortItemKey.ModifiedTime => "\uE823",
			SortItemKey.LastAccessTime => "\uE81C",
			SortItemKey.RegisteredTime => "\uE8F1",
			SortItemKey.FileSize => "\uEDA2",
			SortItemKey.Location => "\uE707",
			SortItemKey.Rate => "\uE734",
			SortItemKey.Resolution => "\uE740",
			SortItemKey.UsageCount => "\uE9D2",
			SortItemKey.Duration => "\uE916",
			SortItemKey.Random => "\uE8B1",
			_ => "\uE8CB"
		};
	}

	/// <summary>
	/// 指定されたソート方向に対応する多言語表示名を取得します。
	/// </summary>
	/// <param name="direction">ソート方向。</param>
	/// <param name="stringProvider">多言語リソースプロバイダー。</param>
	/// <returns>ソート方向の表示名。</returns>
	public static string GetDirectionDisplayName(ListSortDirection direction, IStringProvider stringProvider) {
		return direction == ListSortDirection.Ascending
			? stringProvider.GetString("Sort_Direction_Ascending")
			: stringProvider.GetString("Sort_Direction_Descending");
	}

	/// <summary>
	/// 指定されたソート方向に対応する Segoe Fluent Icons グリフを取得します。
	/// </summary>
	/// <param name="direction">ソート方向。</param>
	/// <returns>アイコングリフ文字列。</returns>
	public static string GetDirectionIconGlyph(ListSortDirection direction) {
		return direction == ListSortDirection.Ascending ? "\uF0AD" : "\uF0AE";
	}

	/// <summary>
	/// ソート項目一覧を簡潔な要約テキストにフォーマットします。
	/// </summary>
	/// <param name="items">ソート項目一覧。</param>
	/// <param name="stringProvider">多言語リソースプロバイダー。</param>
	/// <returns>要約テキスト。</returns>
	public static string FormatSummary(IReadOnlyList<SortItemObject> items, IStringProvider stringProvider) {
		if (items.Count == 0) {
			return stringProvider.GetString("SortManager_NoSortItemsSummary");
		}

		var head = items
			.Take(2)
			.Select(item => $"{GetKeyDisplayName(item.SortItemKey, stringProvider)} ({GetDirectionDisplayName(item.Direction, stringProvider)})");
		var text = string.Join(" → ", head);
		if (items.Count > 2) {
			text = $"{text} {stringProvider.GetString("Common_AndMoreCount", items.Count - 2)}";
		}

		return text;
	}
}