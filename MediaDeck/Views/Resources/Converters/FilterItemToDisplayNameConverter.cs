using CommunityToolkit.Mvvm.DependencyInjection;

using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Core.Models.Files.Filter.FilterItemObjects;

using Microsoft.UI.Xaml.Data;

namespace MediaDeck.Views.Resources.Converters;

/// <summary>
/// フィルターオブジェクトを多言語対応された表示名に変換するコンバータ。
/// </summary>
public class FilterItemToDisplayNameConverter : IValueConverter {
	private IStringProvider? _stringProvider;

	/// <summary>
	/// 多言語リソースプロバイダーのキャッシュ。初回アクセス時に解決される。
	/// </summary>
	private IStringProvider StringProvider {
		get {
			return this._stringProvider ??= Ioc.Default.GetRequiredService<IStringProvider>();
		}
	}

	/// <inheritdoc/>
	public object? Convert(object value, Type targetType, object parameter, string language) {
		if (value is not IFilterItemObject filterItem) {
			return null;
		}

		var stringProvider = this.StringProvider;

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
					_ => throw new ArgumentOutOfRangeException(nameof(value), rf.SearchType, "Unsupported comparison search type.")
				};
				return stringProvider.GetString(rateKey, rf.Rate);

			case ResolutionFilterItemObject resolutionFilter:
				var resOp = resolutionFilter.SearchType switch {
					SearchTypeComparison.GreaterThan => "GreaterThan",
					SearchTypeComparison.GreaterThanOrEqual => "GreaterThanOrEqual",
					SearchTypeComparison.Equal => "Equal",
					SearchTypeComparison.LessThanOrEqual => "LessThanOrEqual",
					SearchTypeComparison.LessThan => "LessThan",
					_ => throw new ArgumentOutOfRangeException(nameof(value), resolutionFilter.SearchType, "Unsupported comparison search type.")
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

	public object ConvertBack(object value, Type targetType, object parameter, string language) {
		throw new NotImplementedException();
	}
}