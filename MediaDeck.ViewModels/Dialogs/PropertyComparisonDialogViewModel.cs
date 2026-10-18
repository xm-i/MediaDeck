using System.Globalization;
using MediaDeck.Common.Base;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Core.Models.Files.SearchConditions;
using MediaDeck.Core.Primitives;

namespace MediaDeck.ViewModels.Dialogs;

/// <summary>
/// プロパティ検索条件（比較演算子と値）を入力・設定するためのダイアログ ViewModel。
/// </summary>
[Inject(InjectServiceLifetime.Transient)]
public class PropertyComparisonDialogViewModel : ViewModelBase {
	private readonly IStringProvider _stringProvider;

	/// <summary>対象プロパティの記述情報を取得します。</summary>
	public MediaItemPropertyDescriptor Descriptor {
		get {
			return this._descriptor ?? throw new InvalidOperationException("ViewModel was not initialized.");
		}
	}

	private MediaItemPropertyDescriptor? _descriptor;

	/// <summary>プロパティ名の表示テキストを取得します。</summary>
	public string PropertyNameText {
		get;
		private set;
	} = string.Empty;

	/// <summary>選択可能な比較演算子の一覧を取得します。</summary>
	public IReadOnlyList<DisplayObject<SearchTypeComparison>> OperatorItems {
		get;
		private set;
	} = [];

	/// <summary>現在選択されている比較演算子を取得します。</summary>
	public ReactiveProperty<DisplayObject<SearchTypeComparison>?> SelectedOperator {
		get;
	}

	/// <summary>文字列入力値を取得します。</summary>
	public ReactiveProperty<string> StringValue { get; } = new("");

	/// <summary>数値入力値を取得します。</summary>
	public ReactiveProperty<double> NumberValue { get; } = new(0.0);

	/// <summary>日時入力値を取得します。</summary>
	public ReactiveProperty<DateTimeOffset?> DateTimeValue { get; } = new(null);

	/// <summary>列挙値入力値を取得します。</summary>
	public ReactiveProperty<object?> EnumValue { get; } = new(null);

	/// <summary>列挙型プロパティの場合の選択肢一覧を取得します。</summary>
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

	/// <summary>
	/// 対象プロパティの記述情報を受け取って ViewModel を初期化します。
	/// </summary>
	/// <param name="descriptor">プロパティ記述情報。</param>
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

	/// <summary>確定後の比較演算子を取得します。</summary>
	public SearchTypeComparison ConfirmedOperator {
		get;
		private set;
	}

	/// <summary>確定後の入力値を取得します。</summary>
	public string ConfirmedValue {
		get;
		private set;
	} = string.Empty;

	/// <summary>
	/// 入力値の検証を行い、確定処理を試みます。
	/// </summary>
	/// <param name="errorMessage">検証に失敗した場合のエラーメッセージ。</param>
	/// <returns>入力値が有効で確定できた場合は <c>true</c>、それ以外は <c>false</c>。</returns>
	public bool TryConfirm(out string? errorMessage) {
		if (this.SelectedOperator.Value == null) {
			errorMessage = this._stringProvider.GetString("PropertyComparisonDialog_Error_SelectOperator");
			return false;
		}
		var raw = this.GetRawValueString();
		if (string.IsNullOrWhiteSpace(raw)) {
			errorMessage = this._stringProvider.GetString("PropertyComparisonDialog_Error_EnterValue");
			return false;
		}
		// 入力値が型として解釈可能か Build で検証する
		if (this.Descriptor.Build(this.SelectedOperator.Value.Value, raw) is null) {
			errorMessage = this._stringProvider.GetString("PropertyComparisonDialog_Error_CannotParseValue", this.Descriptor.ValueType.Name);
			return false;
		}
		this.ConfirmedOperator = this.SelectedOperator.Value.Value;
		this.ConfirmedValue = raw;
		errorMessage = null;
		return true;
	}

	/// <summary>
	/// 入力コントロールに応じた生の文字列値を取得します。
	/// </summary>
	/// <returns>入力値の文字列表現。</returns>
	public string GetRawValueString() {
		if (this.Descriptor.ValueType.IsEnum) {
			return this.EnumValue.Value?.ToString() ?? string.Empty;
		}
		if (this.Descriptor.ValueType == typeof(DateTime) || this.Descriptor.ValueType == typeof(DateTime?)) {
			return this.DateTimeValue.Value?.ToString("yyyy-MM-dd") ?? string.Empty;
		}
		if (IsNumericType(this.Descriptor.ValueType)) {
			return double.IsNaN(this.NumberValue.Value)
				? string.Empty
				: this.NumberValue.Value.ToString(CultureInfo.InvariantCulture);
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