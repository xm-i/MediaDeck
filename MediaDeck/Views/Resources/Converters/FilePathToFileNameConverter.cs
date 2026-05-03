using System.IO;

using Microsoft.UI.Xaml.Data;

namespace MediaDeck.Views.Resources.Converters;

/// <summary>
/// ファイルパスからファイル名（拡張子なし）を取得するConverter
/// </summary>
public class FilePathToFileNameConverter : IValueConverter {
	public object Convert(object value, Type targetType, object parameter, string language) {
		if (value is string path && !string.IsNullOrEmpty(path)) {
			return Path.GetFileNameWithoutExtension(path);
		}
		return string.Empty;
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language) {
		throw new NotImplementedException();
	}
}