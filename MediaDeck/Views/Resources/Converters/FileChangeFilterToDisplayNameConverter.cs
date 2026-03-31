using MediaDeck.ViewModels;

using Microsoft.UI.Xaml.Data;

namespace MediaDeck.Views.Resources.Converters;

/// <summary>
/// FileChangeFilterを表示名文字列に変換するコンバーター
/// </summary>
public class FileChangeFilterToDisplayNameConverter : IValueConverter {
	/// <summary>
	/// FileChangeFilterの列挙値を取得し、対応する日本語表示名に変換します。
	/// </summary>
	/// <param name="value">FileChangeFilter列挙値</param>
	/// <param name="targetType">変換先型（string）</param>
	/// <param name="parameter">変換パラメータ（不使用）</param>
	/// <param name="language">言語（不使用）</param>
	/// <returns>日本語名文字列</returns>
	public object Convert(object? value, Type targetType, object parameter, string language) {
		if (value is FileChangeFilter filter) {
			return filter switch {
				FileChangeFilter.All => "すべて",
				FileChangeFilter.Moved => "移動",
				FileChangeFilter.Deleted => "削除",
				FileChangeFilter.Renamed => "変更",
				FileChangeFilter.Added => "追加",
				_ => value.ToString()
			};
		}
		return value?.ToString() ?? string.Empty;
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language) {
		throw new NotImplementedException();
	}
}