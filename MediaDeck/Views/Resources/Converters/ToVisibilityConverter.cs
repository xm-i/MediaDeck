using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace MediaDeck.Views.Resources.Converters;

/// <summary>
/// 任意の値⇔Visibilityコンバータ
/// </summary>
/// <remarks>
/// 任意の値をVisibilityに変換する
/// </remarks>
public class ToVisibilityConverter : IValueConverter {
	private static readonly object _defaultValue = new();
	/// <summary>
	/// Visibleの場合の値
	/// </summary>
	public object Visible {
		private get;
		set;
	} = _defaultValue;

	/// <summary>
	/// Collapseの場合の値
	/// </summary>
	public object Collapse {
		private get;
		set;
	} = _defaultValue;

	/// <summary>
	/// コンバート
	/// </summary>
	/// <param name="value">変換前値</param>
	/// <param name="targetType">未使用</param>
	/// <param name="parameter">未使用</param>
	/// <param name="language">未使用</param>
	/// <returns>変換後値(<see cref="Visibility"/>)</returns>
	public object Convert(object value, Type targetType, object parameter, string language) {
		if (value == this.Visible && value != _defaultValue) {
			return Visibility.Visible;
		} else if (value == this.Collapse && value != _defaultValue) {
			return Visibility.Collapsed;
		} else if (this.Visible is string v && v == "*") {
			return Visibility.Visible;
		} else if (this.Collapse is string c && c == "*") {
			return Visibility.Collapsed;
		}
		throw new InvalidOperationException();
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language) {
		throw new NotImplementedException();
	}
}