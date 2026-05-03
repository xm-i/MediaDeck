using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace MediaDeck.Views.Resources.Converters;

/// <summary>
/// bool値に基づいてオーバーレイありサムネイルの表示/非表示を制御するConverter
/// true → Visible / false → Collapsed
/// </summary>
public class CardDisplayModeToOverlayVisibilityConverter : IValueConverter {
	public object Convert(object value, Type targetType, object parameter, string language) {
		if (value is bool showOverlay) {
			return showOverlay ? Visibility.Visible : Visibility.Collapsed;
		}
		return Visibility.Visible;
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language) {
		throw new NotImplementedException();
	}
}