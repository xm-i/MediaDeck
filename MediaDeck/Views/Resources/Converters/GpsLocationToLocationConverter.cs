using MapControl;

using MediaDeck.Composition.Interfaces.Primitives;
using MediaDeck.Composition.Objects;

using Microsoft.UI.Xaml.Data;

namespace MediaDeck.Views.Resources.Converters;

public class GpsLocationToLocationConverter : IValueConverter {
	public object? Convert(object value, Type targetType, object parameter, string language) {
		if (value is IGpsLocation gps) {
			return new Location(gps.Latitude, gps.Longitude);
		}
		return null;
	}

	public object? ConvertBack(object value, Type targetType, object parameter, string language) {
		if (value is Location loc) {
			return new GpsLocation(loc.Latitude, loc.Longitude);
		}
		return null;
	}
}