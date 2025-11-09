using System.Collections.Generic;

using Microsoft.UI.Xaml.Data;

namespace MediaDeck.Views.Resources.Converters;

public abstract class DictionaryConverterBase<TKey,TValue> : IValueConverter where TKey:notnull  {
	protected Dictionary<TKey, TValue> Dictionary {
		get;
		set;
	} = [];

	public object? Convert(object value, Type targetType, object parameter, string language) {
		if(value is TKey key && this.Dictionary.TryGetValue(key,out var result)) {
			return result;
		}
		return null;
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language) {
		throw new NotImplementedException();
	}

}
