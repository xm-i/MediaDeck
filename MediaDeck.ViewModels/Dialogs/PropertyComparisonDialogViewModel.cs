using MediaDeck.Common.Base;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Core.Models.Files.SearchConditions;
using MediaDeck.Core.Primitives;

namespace MediaDeck.ViewModels.Dialogs;

[Inject(InjectServiceLifetime.Transient)]
public class PropertyComparisonDialogViewModel : ViewModelBase {
	private readonly IStringProvider _stringProvider;

	public MediaItemPropertyDescriptor Descriptor {
		get {
			return this._descriptor ?? throw new InvalidOperationException("ViewModel was not initialized.");
		}
	}

	private MediaItemPropertyDescriptor? _descriptor;

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

	/// <summary>
	/// <see cref="PropertyComparisonDialogViewModel"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="stringProvider">文字列プロバイダー</param>
	public PropertyComparisonDialogViewModel(IStringProvider stringProvider) {
		this._stringProvider = stringProvider;
		this.SelectedOperator = new ReactiveProperty<DisplayObject<SearchTypeComparison>?>().AddTo(this.CompositeDisposable);
		this.StringValue.AddTo(this.CompositeDisposable);
		this.NumberValue.AddTo(this.CompositeDisposable);
		this.DateTimeValue.AddTo(this.CompositeDisposable);
		this.EnumValue.AddTo(this.CompositeDisposable);
	}

	public void Initialize(MediaItemPropertyDescriptor descriptor) {
		this._descriptor = descriptor;
		this.PropertyNameText = $"prop.{descriptor.Name}  ({descriptor.ValueType.Name})";

		this.OperatorItems = descriptor.SupportedOperators
			.Select(op => new DisplayObject<SearchTypeComparison>(this.GetOperatorLabel(op), op))
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

	/// <summary>
	/// 比較演算子の表示ラベルを取得します。
	/// </summary>
	/// <param name="op">比較演算子</param>
	/// <returns>表示ラベル</returns>
	private string GetOperatorLabel(SearchTypeComparison op) {
		return op switch {
			SearchTypeComparison.GreaterThan => $"{this._stringProvider.GetString("FilterCreator_Comparison_GreaterThan")} (>)",
			SearchTypeComparison.GreaterThanOrEqual => $"{this._stringProvider.GetString("FilterCreator_Comparison_GreaterThanOrEqual")} (>=)",
			SearchTypeComparison.Equal => $"{this._stringProvider.GetString("FilterCreator_Comparison_Equal")} (=)",
			SearchTypeComparison.LessThanOrEqual => $"{this._stringProvider.GetString("FilterCreator_Comparison_LessThanOrEqual")} (<=)",
			SearchTypeComparison.LessThan => $"{this._stringProvider.GetString("FilterCreator_Comparison_LessThan")} (<)",
			SearchTypeComparison.Contains => $"{this._stringProvider.GetString("FilterCreator_Comparison_Contains")} (Contains)",
			_ => op.ToString()
		};
	}
}