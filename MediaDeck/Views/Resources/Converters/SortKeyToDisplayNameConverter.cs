using Microsoft.UI.Xaml.Data;
using System.Collections.Generic;
using MediaDeck.Composition.Enum;

namespace MediaDeck.Views.Resources.Converters; 

public class SortKeyToDisplayNameConverter : IValueConverter {
	private static readonly Dictionary<SortItemKey, string> _names = new() {
		{SortItemKey.FilePath,"File Path" },
		{SortItemKey.CreationTime,"Creation Time" },
		{SortItemKey.ModifiedTime,"Modified Time" },
		{SortItemKey.LastAccessTime,"Last Access Time" },
		{SortItemKey.RegisteredTime,"Registered Time" },
		{SortItemKey.FileSize,"File Size" },
		{SortItemKey.Location,"Location" },
		{SortItemKey.Rate,"Rate" },
		{SortItemKey.Resolution,"Resolution" },
		{SortItemKey.UsageCount,"Usage Count" },
	};

	public object? Convert(object value, Type targetType, object parameter, string language) {
		if (value is SortItemKey sik) {
			return _names[sik];
		}

		return null;
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language) {
		throw new NotImplementedException();
	}

}
