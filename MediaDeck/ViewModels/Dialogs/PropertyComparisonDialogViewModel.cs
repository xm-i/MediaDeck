using System.Collections.Generic;

using MediaDeck.Common.Base;
using MediaDeck.Composition.Enum;
using MediaDeck.Core.Models.Files.SearchConditions;
using MediaDeck.Core.Primitives;

namespace MediaDeck.ViewModels.Dialogs;

[Inject(InjectServiceLifetime.Transient)]
public class PropertyComparisonDialogViewModel : ViewModelBase {
	public MediaItemPropertyDescriptor? Descriptor {
		get;
		private set;
	}

	public string PropertyNameText {
		get;
		private set;
	} = string.Empty;

	public IReadOnlyList<DisplayObject<SearchTypeComparison>> OperatorItems {
		get;
		private set;
	} = [];

	public ReactiveProperty<DisplayObject<SearchTypeComparison>?> SelectedOperator {
		get;
	}

	// 型ごとの入力値バインディング用プロパティ
	public ReactiveProperty<string> StringValue { get; } = new("");
	public ReactiveProperty<double> NumberValue { get; } = new(0.0);
	public ReactiveProperty<DateTimeOffset?> DateTimeValue { get; } = new(null);
	public ReactiveProperty<object?> EnumValue { get; } = new(null);

	public IReadOnlyList<DisplayObject<object>> EnumItems {
		get;
		private set;
	} = [];

	public PropertyComparisonDialogViewModel() {
		this.SelectedOperator = new ReactiveProperty<DisplayObject<SearchTypeComparison>?>().AddTo(this.CompositeDisposable);
		this.StringValue.AddTo(this.CompositeDisposable);
		this.NumberValue.AddTo(this.CompositeDisposable);
		this.DateTimeValue.AddTo(this.CompositeDisposable);
		this.EnumValue.AddTo(this.CompositeDisposable);
	}

	public void Initialize(MediaItemPropertyDescriptor descriptor) {
		this.Descriptor = descriptor;
		this.PropertyNameText = $"prop.{descriptor.Name}  ({descriptor.ValueType.Name})";

		this.OperatorItems = descriptor.SupportedOperators
			.Select(op => new DisplayObject<SearchTypeComparison>(GetOperatorLabel(op), op))
			.ToList();

		this.SelectedOperator.Value = this.OperatorItems.FirstOrDefault();

		if (descriptor.ValueType.IsEnum) {
			this.EnumItems = Enum.GetValues(descriptor.ValueType)
				.Cast<object>()
				.Select(v => new DisplayObject<object>(v.ToString() ?? string.Empty, v))
				.ToList();
			this.EnumValue.Value = this.EnumItems.FirstOrDefault()?.Value;
		} else {
			this.EnumItems = Array.Empty<DisplayObject<object>>();
		}
	}

	public string GetRawValueString() {
		if (this.Descriptor.ValueType.IsEnum) {
			return this.EnumValue.Value?.ToString() ?? string.Empty;
		}
		if (this.Descriptor.ValueType == typeof(DateTime) || this.Descriptor.ValueType == typeof(DateTime?)) {
			return this.DateTimeValue.Value?.ToString("yyyy-MM-dd") ?? string.Empty;
		}
		if (IsNumericType(this.Descriptor.ValueType)) {
			return this.NumberValue.Value.ToString();
		}
		return this.StringValue.Value ?? string.Empty;
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

	private static string GetOperatorLabel(SearchTypeComparison op) {
		return op switch {
			SearchTypeComparison.GreaterThan => "を超える (>)",
			SearchTypeComparison.GreaterThanOrEqual => "以上 (>=)",
			SearchTypeComparison.Equal => "と等しい (=)",
			SearchTypeComparison.LessThanOrEqual => "以下 (<=)",
			SearchTypeComparison.LessThan => "未満 (<)",
			SearchTypeComparison.Contains => "を含む (Contains)",
			_ => op.ToString()
		};
	}
}