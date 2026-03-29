using Microsoft.UI.Xaml.Data;

namespace MediaDeck.Views.Resources.Converters;

/// <summary>
/// Null値をBooleanに変換するコンバーター
/// </summary>
public class NullToBooleanConverter : IValueConverter {
	/// <summary>
	/// Nullの場合にfalse、それ以外はtrueを返す
	/// </summary>
	public object Convert(object? value, Type targetType, object parameter, string language) {
		return value != null;
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language) {
		throw new NotImplementedException();
	}
}