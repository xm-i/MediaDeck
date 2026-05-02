using Microsoft.UI.Xaml.Data;

namespace MediaDeck.Views.Resources.Converters;

/// <summary>
/// オブジェクトの等価性を判定し、bool値を返すコンバーター。
/// ConverterParameterに比較対象を指定する。
/// </summary>
public class EqualityToBooleanConverter : IValueConverter {
	public object Convert(object value, Type targetType, object parameter, string language) {
		if (value == null || parameter == null) {
			return false;
		}

		if (value is Enum e) {
			var name = e.ToString();
			var val = System.Convert.ToInt32(e).ToString();
			var p = parameter.ToString();
			return string.Equals(name, p, StringComparison.OrdinalIgnoreCase) ||
				   string.Equals(val, p, StringComparison.OrdinalIgnoreCase);
		}

		return string.Equals(value.ToString(), parameter.ToString(), StringComparison.OrdinalIgnoreCase);
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language) {
		return value is true ? parameter : Microsoft.UI.Xaml.DependencyProperty.UnsetValue;
	}
}