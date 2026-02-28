using Microsoft.UI.Xaml.Data;

namespace MediaDeck.Views.Resources.Converters;

public class FileSizeConverter : IValueConverter {
	private static readonly string[] _units = ["B", "KB", "MB", "GB", "TB"];

	public object Convert(object value, Type targetType, object parameter, string language) {
		if (value is not long size || size <= 0) {
			return "-";
		}

		var unitIndex = 0;
		var displaySize = (double)size;

		while (displaySize >= 1024 && unitIndex < _units.Length - 1) {
			displaySize /= 1024;
			unitIndex++;
		}

		return unitIndex == 0
			? $"{displaySize:F0} {_units[unitIndex]}"
			: $"{displaySize:F2} {_units[unitIndex]}";
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language) {
		throw new NotImplementedException();
	}
}
