using System.Collections.Generic;
using System.Linq;

using MediaDeck.Composition.Interfaces.Files;

using Microsoft.UI.Xaml.Data;

namespace MediaDeck.Views.Resources.Converters;

/// <summary>
/// タグリストをカンマ区切り文字列に変換するコンバーター
/// </summary>
public class TagListConverter : IValueConverter {
	/// <summary>
	/// x:Bind用の静的変換メソッド
	/// </summary>
	public static string Convert(IEnumerable<ITagModel>? tags) {
		if (tags is null || !tags.Any()) {
			return "-";
		}
		return string.Join(", ", tags.Select(t => t.TagName));
	}

	public object Convert(object value, Type targetType, object parameter, string language) {
		if (value is not IEnumerable<ITagModel> tags || !tags.Any()) {
			return "-";
		}

		return string.Join(", ", tags.Select(t => t.TagName));
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language) {
		throw new NotImplementedException();
	}
}