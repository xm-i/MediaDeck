using Microsoft.UI.Xaml.Data;
using MediaDeck.ViewModels;
using System;

namespace MediaDeck.Views.Resources.Converters;

/// <summary>
/// FileChangeFilterを表示名文字列に変換するコンバーター
/// </summary>
public class FileChangeFilterToDisplayNameConverter : IValueConverter {
	public object Convert(object value, Type targetType, object parameter, string language) {
		if (value is FileChangeFilter filter) {
			return filter switch {
				FileChangeFilter.All => "すべて",
				FileChangeFilter.Moved => "移動",
				FileChangeFilter.Deleted => "削除",
				_ => value.ToString()
			};
		}
		return value?.ToString() ?? string.Empty;
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language) {
		throw new NotImplementedException();
	}
}
