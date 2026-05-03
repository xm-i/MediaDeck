using Microsoft.UI.Xaml.Data;

namespace MediaDeck.MediaItemTypes.UI.Base.Converters;

/// <summary>
/// 秒数（double?）を短い時間文字列（HH:MM:SS or MM:SS）に変換するConverter
/// </summary>
public class DurationToStringConverter : IValueConverter {
	public object Convert(object value, Type targetType, object parameter, string language) {
		if (value is double seconds && !double.IsNaN(seconds)) {
			var ts = TimeSpan.FromSeconds(seconds);
			return ts.TotalHours >= 1
				? ts.ToString(@"h\:mm\:ss")
				: ts.ToString(@"m\:ss");
		}
		return string.Empty;
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language) {
		throw new NotImplementedException();
	}
}