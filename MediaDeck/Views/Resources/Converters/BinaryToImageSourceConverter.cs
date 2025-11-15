using System.Threading.Tasks;

using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;

using Windows.Storage.Streams;

namespace MediaDeck.Views.Resources.Converters;

public class BinaryToImageSourceConverter : IValueConverter {
	public object? Convert(object value, Type targetType, object parameter, string language) {
		if (value is byte[] binary) {
			var bi = new BitmapImage();
			using var stream = new InMemoryRandomAccessStream();
			using var writer = new DataWriter(stream);
			writer.WriteBytes(binary);
			Task.Run(async () => {
				await writer.StoreAsync();
				await writer.FlushAsync();
			}).Wait();
			writer.DetachStream();
			stream.Seek(0);

			bi.SetSource(stream);
			return bi;
		}
		return null;
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language) {
		throw new NotImplementedException();
	}
}
