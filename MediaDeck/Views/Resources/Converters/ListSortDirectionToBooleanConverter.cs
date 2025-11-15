using System.ComponentModel;

using Microsoft.UI.Xaml.Data;

namespace MediaDeck.Views.Resources.Converters;

public class ListSortDirectionToBooleanConverter : IValueConverter {
	/// <summary>
	/// Trueにするほう
	/// </summary>
	public ListSortDirection ConvertWhenTrue {
		private get;
		set;
	} = ListSortDirection.Ascending;

	/// <summary>
	/// コンバート
	/// </summary>
	/// <param name="value">変換前値(<see cref="ListSortDirection"/>)</param>
	/// <param name="targetType">未使用</param>
	/// <param name="parameter">未使用</param>
	/// <param name="language">未使用</param>
	/// <returns>変換後値(<see cref="bool"/>)</returns>
	public object Convert(object value, Type targetType, object parameter, string language) {
		if (value is ListSortDirection listSortDirection) {
			return listSortDirection == this.ConvertWhenTrue;
		}
		return false;
	}

	/// <summary>
	/// コンバートバック
	/// </summary>
	/// <param name="value">変換前値(<see cref="bool"/>)</param>
	/// <param name="targetType">未使用</param>
	/// <param name="parameter">未使用</param>
	/// <param name="language">未使用</param>
	/// <returns>変換後値(<see cref="ListSortDirection"/>)</returns>
	public object ConvertBack(object value, Type targetType, object parameter, string language) {
		var b = false;
		if (value is bool bb) {
			b = bb;
		}
		if (this.ConvertWhenTrue != ListSortDirection.Ascending) {
			b = !b;
		}
		return b ? ListSortDirection.Ascending : ListSortDirection.Descending;
	}
}