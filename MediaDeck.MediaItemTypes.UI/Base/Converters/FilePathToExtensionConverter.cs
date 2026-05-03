using System.IO;

using Microsoft.UI.Xaml.Data;

namespace MediaDeck.MediaItemTypes.UI.Base.Converters;

/// <summary>
/// ファイルパスから拡張子（大文字、ドットなし）を取得するConverter
/// </summary>
public class FilePathToExtensionConverter : IValueConverter {
	public object Convert(object value, Type targetType, object parameter, string language) {
		if (value is string path && !string.IsNullOrEmpty(path)) {
			return Path.GetExtension(path).TrimStart('.').ToUpperInvariant();
		}
		return string.Empty;
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language) {
		throw new NotImplementedException();
	}
}