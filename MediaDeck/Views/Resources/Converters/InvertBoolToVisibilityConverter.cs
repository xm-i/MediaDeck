using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace MediaDeck.Views.Resources.Converters;

/// <summary>
/// BooleanToVisibilityConverterの逆
/// </summary>
public class InvertBoolToVisibilityConverter : IValueConverter {
	/// <summary>
	/// コンバート
	/// </summary>
	/// <param name="value">変換前値</param>
	/// <param name="targetType">未使用</param>
	/// <param name="parameter">未使用</param>
	/// <param name="language">未使用</param>
	/// <returns>変換後値(<see cref="Visibility"/>)</returns>
	public object Convert(object value, Type targetType, object parameter, string language) {
		if (value is bool b) {
			return b ? Visibility.Collapsed : Visibility.Visible;
		}
		return Visibility.Visible;
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language) {
		if (value is Visibility v) {
			return v == Visibility.Collapsed;
		}
		return false;
	}
}