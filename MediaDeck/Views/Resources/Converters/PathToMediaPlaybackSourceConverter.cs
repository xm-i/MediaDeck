
using Microsoft.UI.Xaml.Data;

using Windows.Media.Core;

namespace MediaDeck.Views.Resources.Converters;

public class PathToMediaPlaybackSourceConverter : IValueConverter {
	public object? Convert(object value, Type targetType, object parameter, string language) {
		if (value is string path) {
			return MediaSource.CreateFromUri(new Uri(path));
		}
		return null;
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language) {
		throw new NotImplementedException();
	}
}
