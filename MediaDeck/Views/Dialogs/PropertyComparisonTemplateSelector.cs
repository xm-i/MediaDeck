using MediaDeck.ViewModels.Dialogs;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MediaDeck.Views.Dialogs;

public sealed class PropertyComparisonTemplateSelector : DataTemplateSelector {
	public DataTemplate? StringTemplate {
		get; set;
	}
	public DataTemplate? NumberTemplate {
		get; set;
	}
	public DataTemplate? DateTimeTemplate {
		get; set;
	}
	public DataTemplate? EnumTemplate {
		get; set;
	}

	protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container) {
		if (item is PropertyComparisonDialogViewModel vm) {
			var type = vm.Descriptor.ValueType;
			if (type.IsEnum) {
				return this.EnumTemplate;
			}
			if (type == typeof(DateTime) || type == typeof(DateTime?)) {
				return this.DateTimeTemplate;
			}
			if (IsNumericType(type)) {
				return this.NumberTemplate;
			}
			return this.StringTemplate;
		}
		return base.SelectTemplateCore(item, container);
	}

	private static bool IsNumericType(Type type) {
		type = Nullable.GetUnderlyingType(type) ?? type;
		return type == typeof(byte) || type == typeof(sbyte) ||
			   type == typeof(short) || type == typeof(ushort) ||
			   type == typeof(int) || type == typeof(uint) ||
			   type == typeof(long) || type == typeof(ulong) ||
			   type == typeof(float) || type == typeof(double) ||
			   type == typeof(decimal);
	}
}