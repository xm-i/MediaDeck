using CommunityToolkit.Mvvm.DependencyInjection;

using MediaDeck.Composition.Interfaces;
using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Core.Models.Files.Filter;

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

		return FilterItemDisplayHelper.FormatDisplayText(filterItem, this.StringProvider);
	}

	/// <inheritdoc/>
	public object ConvertBack(object value, Type targetType, object parameter, string language) {
		throw new NotImplementedException();
	}
}