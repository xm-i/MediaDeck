using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace MediaDeck.Views.Resources.Converters;

public class TimeSpanToStringConverter : IValueConverter {
	/// <summary>
	/// コンバート
	/// </summary>
	/// <param name="value">変換前値</param>
	/// <param name="targetType">未使用</param>
	/// <param name="parameter">未使用</param>
	/// <param name="language">未使用</param>
	/// <returns>変換後値(<see cref="Visibility"/>)</returns>
	public object? Convert(object value, Type targetType, object parameter, string language) {
		if(value is TimeSpan timeSpan) {
			return timeSpan.ToString(@"hh\:mm\:ss");
		}
		return null;
	}

	public object? ConvertBack(object value, Type targetType, object parameter, string language) {
		if (value is string str) {
			if(TimeSpan.TryParse(str, out var timeSpan)) {
				return timeSpan;
			}
		}
		return null;
	}
}