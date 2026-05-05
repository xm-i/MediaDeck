using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace MediaDeck.Views.Panes.ViewerPanes;

public sealed class DoubleToGridLengthConverter : IValueConverter {
	public object Convert(object value, Type targetType, object parameter, string language) {
		if (value is double width) {
			return new GridLength(Math.Max(0, width));
		}
		if (value is float widthFloat) {
			return new GridLength(Math.Max(0, widthFloat));
		}
		return new GridLength(0);
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language) {
		if (value is GridLength gridLength) {
			return gridLength.Value;
		}
		return 0d;
	}
}