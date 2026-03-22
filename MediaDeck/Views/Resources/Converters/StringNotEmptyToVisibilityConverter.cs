using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace MediaDeck.Views.Resources.Converters;

/// <summary>
/// 文字列が空でない場合にVisibleを返すコンバーター
/// </summary>
public class StringNotEmptyToVisibilityConverter : IValueConverter {
	public object Convert(object value, Type targetType, object parameter, string language) {
		if (value is string s && !string.IsNullOrWhiteSpace(s)) {
			return Visibility.Visible;
		}
		return Visibility.Collapsed;
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language) {
		throw new NotImplementedException();
	}
}
