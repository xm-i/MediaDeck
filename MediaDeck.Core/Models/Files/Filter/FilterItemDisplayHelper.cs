using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Core.Models.Files.Filter.FilterItemObjects;

namespace MediaDeck.Core.Models.Files.Filter;

/// <summary>
/// フィルター項目の表示名・カテゴリ名・アイコン・要約テキストを生成するヘルパークラス。
/// </summary>
public static class FilterItemDisplayHelper {
	/// <summary>
	/// フィルター項目を多言語対応された表示テキストへ変換します。
	/// </summary>
	/// <param name="filterItem">フィルター項目オブジェクト。</param>
	/// <param name="stringProvider">多言語リソースプロバイダー。</param>
	/// <returns>表示テキスト。</returns>
	public static string FormatDisplayText(IFilterItemObject filterItem, IStringProvider stringProvider) {
		switch (filterItem) {
			case ExistsFilterItemObject ef:
				return ef.Exists
					? stringProvider.GetString("FilterItem_Exists_True")
					: stringProvider.GetString("FilterItem_Exists_False");

			case FilePathFilterItemObject fpf:
				return fpf.SearchType == SearchTypeInclude.Include
					? stringProvider.GetString("FilterItem_FilePath_Include", fpf.Text)
					: stringProvider.GetString("FilterItem_FilePath_Exclude", fpf.Text);

			case FolderGroupFilterItemObject fgf:
				return fgf.SearchType == SearchTypeInclude.Include
					? stringProvider.GetString("FilterItem_FolderGroup_Include")
					: stringProvider.GetString("FilterItem_FolderGroup_Exclude");

			case LocationFilterItemObject lf:
				if (lf.Text != null) {
					return stringProvider.GetString("FilterItem_Location_Text", lf.Text);
				}
				if (lf.Contains is { } b) {
					return b
						? stringProvider.GetString("FilterItem_Location_Contains_True")
						: stringProvider.GetString("FilterItem_Location_Contains_False");
				}
				if (lf.LeftTop != null && lf.RightBottom != null) {
					return stringProvider.GetString("FilterItem_Location_Range", lf.LeftTop, lf.RightBottom);
				}
				return "Error";

			case MediaTypeFilterItemObject mtf:
				return mtf.IsVideo
					? stringProvider.GetString("FilterItem_MediaType_Video")
					: stringProvider.GetString("FilterItem_MediaType_Image");

			case RateFilterItemObject rf:
				var rateKey = rf.SearchType switch {
					SearchTypeComparison.GreaterThan => "FilterItem_Rate_GreaterThan",
					SearchTypeComparison.GreaterThanOrEqual => "FilterItem_Rate_GreaterThanOrEqual",
					SearchTypeComparison.Equal => "FilterItem_Rate_Equal",
					SearchTypeComparison.LessThanOrEqual => "FilterItem_Rate_LessThanOrEqual",
					SearchTypeComparison.LessThan => "FilterItem_Rate_LessThan",
					_ => throw new ArgumentOutOfRangeException(nameof(filterItem), rf.SearchType, "Unsupported comparison search type.")
				};
				return stringProvider.GetString(rateKey, rf.Rate);

			case ResolutionFilterItemObject resolutionFilter:
				var resOp = resolutionFilter.SearchType switch {
					SearchTypeComparison.GreaterThan => "GreaterThan",
					SearchTypeComparison.GreaterThanOrEqual => "GreaterThanOrEqual",
					SearchTypeComparison.Equal => "Equal",
					SearchTypeComparison.LessThanOrEqual => "LessThanOrEqual",
					SearchTypeComparison.LessThan => "LessThan",
					_ => throw new ArgumentOutOfRangeException(nameof(filterItem), resolutionFilter.SearchType, "Unsupported comparison search type.")
				};

				if (resolutionFilter.Width is { } w) {
					return stringProvider.GetString($"FilterItem_Resolution_Width_{resOp}", w);
				}
				if (resolutionFilter.Height is { } h) {
					return stringProvider.GetString($"FilterItem_Resolution_Height_{resOp}", h);
				}
				if (resolutionFilter.Resolution is { } r) {
					return stringProvider.GetString($"FilterItem_Resolution_Area_{resOp}", r);
				}
				return "Error";

			case TagFilterItemObject tf:
				return tf.SearchType == SearchTypeInclude.Include
					? stringProvider.GetString("FilterItem_Tag_Include", tf.TagName)
					: stringProvider.GetString("FilterItem_Tag_Exclude", tf.TagName);

			default:
				return filterItem.DisplayName;
		}
	}

	/// <summary>
	/// フィルター項目に対応する短いカテゴリ名を取得します。
	/// </summary>
	/// <param name="filterItem">フィルター項目オブジェクト。</param>
	/// <param name="stringProvider">多言語リソースプロバイダー。</param>
	/// <returns>カテゴリ名。</returns>
	public static string GetCategoryTitle(IFilterItemObject filterItem, IStringProvider stringProvider) {
		return filterItem switch {
			TagFilterItemObject => stringProvider.GetString("FilterCreator_Tag_ShortTitle"),
			FilePathFilterItemObject => stringProvider.GetString("FilterCreator_FilePath_ShortTitle"),
			MediaTypeFilterItemObject => stringProvider.GetString("FilterCreator_MediaType_ShortTitle"),
			RateFilterItemObject => stringProvider.GetString("FilterCreator_Rate_ShortTitle"),
			ResolutionFilterItemObject => stringProvider.GetString("FilterCreator_Resolution_ShortTitle"),
			LocationFilterItemObject => stringProvider.GetString("FilterCreator_Location_ShortTitle"),
			FolderGroupFilterItemObject => stringProvider.GetString("FilterCreator_FolderGroup_ShortTitle"),
			ExistsFilterItemObject => stringProvider.GetString("FilterCreator_Exists_ShortTitle"),
			_ => stringProvider.GetString("Filter_Header.Text")
		};
	}

	/// <summary>
	/// フィルター項目に対応する Segoe Fluent Icons グリフを取得します。
	/// </summary>
	/// <param name="filterItem">フィルター項目オブジェクト。</param>
	/// <returns>アイコングリフ文字列。</returns>
	public static string GetCategoryIconGlyph(IFilterItemObject filterItem) {
		return filterItem switch {
			TagFilterItemObject => "\uE8EC",
			FilePathFilterItemObject => "\uE8A5",
			MediaTypeFilterItemObject => "\uE8B9",
			RateFilterItemObject => "\uE734",
			ResolutionFilterItemObject => "\uE740",
			LocationFilterItemObject => "\uE707",
			FolderGroupFilterItemObject => "\uED41",
			ExistsFilterItemObject => "\uE9D5",
			_ => "\uE71C"
		};
	}

	/// <summary>
	/// フィルター項目一覧を簡潔な要約テキストにフォーマットします。
	/// </summary>
	/// <param name="items">フィルター項目一覧。</param>
	/// <param name="stringProvider">多言語リソースプロバイダー。</param>
	/// <returns>要約テキスト。</returns>
	public static string FormatSummary(IReadOnlyList<IFilterItemObject> items, IStringProvider stringProvider) {
		if (items.Count == 0) {
			return stringProvider.GetString("FilterManager_NoFilterItemsSummary");
		}

		var firstText = FormatDisplayText(items[0], stringProvider);
		if (items.Count == 1) {
			return firstText;
		}

		return $"{firstText} {stringProvider.GetString("Common_AndMoreCount", items.Count - 1)}";
	}

	/// <summary>
	/// フィルター項目一覧の全ルールを改行区切りのツールチップテキストにフォーマットします。
	/// </summary>
	/// <param name="items">フィルター項目一覧。</param>
	/// <param name="stringProvider">多言語リソースプロバイダー。</param>
	/// <returns>詳細ツールチップテキスト。</returns>
	public static string FormatDetailTooltip(IReadOnlyList<IFilterItemObject> items, IStringProvider stringProvider) {
		if (items.Count == 0) {
			return stringProvider.GetString("FilterManager_NoFilterItemsSummary");
		}

		return string.Join(Environment.NewLine, items.Select(item => $"• {FormatDisplayText(item, stringProvider)}"));
	}
}