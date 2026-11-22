using MediaDeck.Common.Base;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Composition.Objects;
using MediaDeck.Core.Models.Files.Filter.FilterItemObjects;
using MediaDeck.Core.Primitives;

namespace MediaDeck.ViewModels.Filters.FilterItemCreators;

/// <summary>
/// 解像度フィルター作成ViewModel
/// </summary>
public class ResolutionFilterCreatorViewModel : ViewModelBase, IFilterCreatorViewModel {
	private readonly IStringProvider _stringProvider;

	/// <summary>
	/// <see cref="ResolutionFilterCreatorViewModel"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="target">追加先のフィルター条件編集 ViewModel。</param>
	/// <param name="stringProvider">文字列リソースプロバイダー。</param>
	public ResolutionFilterCreatorViewModel(ReactiveProperty<FilteringConditionEditorViewModel?> target, IStringProvider stringProvider) {
		this._stringProvider = stringProvider;
		this.IsSelected = new BindableReactiveProperty<bool>(false).AddTo(this.CompositeDisposable);
		this.WidthValue = new BindableReactiveProperty<double>(double.NaN).AddTo(this.CompositeDisposable);
		this.HeightValue = new BindableReactiveProperty<double>(double.NaN).AddTo(this.CompositeDisposable);
		this.SearchType = new BindableReactiveProperty<DisplayObject<SearchTypeComparison>?>().AddTo(this.CompositeDisposable);

		this.SearchTypeList = [
			new DisplayObject<SearchTypeComparison>(this._stringProvider.GetString("FilterCreator_Comparison_GreaterThan"), SearchTypeComparison.GreaterThan),
			new DisplayObject<SearchTypeComparison>(this._stringProvider.GetString("FilterCreator_Comparison_GreaterThanOrEqual"), SearchTypeComparison.GreaterThanOrEqual),
			new DisplayObject<SearchTypeComparison>(this._stringProvider.GetString("FilterCreator_Comparison_Equal"), SearchTypeComparison.Equal),
			new DisplayObject<SearchTypeComparison>(this._stringProvider.GetString("FilterCreator_Comparison_LessThanOrEqual"), SearchTypeComparison.LessThanOrEqual),
			new DisplayObject<SearchTypeComparison>(this._stringProvider.GetString("FilterCreator_Comparison_LessThan"), SearchTypeComparison.LessThan)
		];
		this.SearchType.Value = this.SearchTypeList.First(x => x.Value == SearchTypeComparison.GreaterThanOrEqual);

		this.ApplyPresetCommand = new ReactiveCommand<string>().AddTo(this.CompositeDisposable);
		this.ApplyPresetCommand.Subscribe(preset => {
			switch (preset) {
				case "HD":
					this.WidthValue.Value = 1280;
					this.HeightValue.Value = 720;
					break;
				case "FHD":
					this.WidthValue.Value = 1920;
					this.HeightValue.Value = 1080;
					break;
				case "4K":
					this.WidthValue.Value = 3840;
					this.HeightValue.Value = 2160;
					break;
			}
		}).AddTo(this.CompositeDisposable);

		this.ClearResolutionCommand = new ReactiveCommand().AddTo(this.CompositeDisposable);
		this.ClearResolutionCommand.Subscribe(_ => {
			this.WidthValue.Value = double.NaN;
			this.HeightValue.Value = double.NaN;
		}).AddTo(this.CompositeDisposable);

		this.AddFilterCommand = this.WidthValue
			.CombineLatest(
				this.HeightValue,
				this.SearchType,
				(w, h, st) => TryParseDimension(w, out var pw) && TryParseDimension(h, out var ph) && (pw.HasValue || ph.HasValue) && st is not null)
			.ToReactiveCommand()
			.AddTo(this.CompositeDisposable);

		this.AddFilterCommand.Subscribe(_ => {
			if (!TryParseDimension(this.WidthValue.Value, out var width) ||
				!TryParseDimension(this.HeightValue.Value, out var height) ||
				(!width.HasValue && !height.HasValue) ||
				this.SearchType.Value is not { } searchType) {
				return;
			}

			IFilterItemObject filterItemObject;
			if (width is { } w2 && height is { } h2) {
				filterItemObject = new ResolutionFilterItemObject {
					Resolution = new ComparableSize(w2, h2),
					SearchType = searchType.Value
				};
			} else {
				filterItemObject = new ResolutionFilterItemObject {
					Width = width,
					Height = height,
					SearchType = searchType.Value
				};
			}
			target.Value?.AddFilter(filterItemObject);
		}).AddTo(this.CompositeDisposable);
	}

	/// <inheritdoc />
	public FilterCreatorKind Kind {
		get {
			return FilterCreatorKind.Resolution;
		}
	}

	/// <inheritdoc />
	public string Title {
		get {
			return this._stringProvider.GetString("FilterCreator_Resolution_Title");
		}
	}

	/// <inheritdoc />
	public string ShortTitle {
		get {
			return this._stringProvider.GetString("FilterCreator_Resolution_ShortTitle");
		}
	}

	/// <inheritdoc />
	public string IconGlyph {
		get {
			return "\uE740";
		}
	}

	/// <inheritdoc />
	public string Description {
		get {
			return this._stringProvider.GetString("FilterCreator_Resolution_Description");
		}
	}

	/// <inheritdoc />
	public BindableReactiveProperty<bool> IsSelected {
		get;
	}

	/// <summary>
	/// 解像度の幅（未指定時は <see cref="double.NaN"/>）を取得します。
	/// </summary>
	public BindableReactiveProperty<double> WidthValue {
		get;
	}

	/// <summary>
	/// 解像度の高さ（未指定時は <see cref="double.NaN"/>）を取得します。
	/// </summary>
	public BindableReactiveProperty<double> HeightValue {
		get;
	}

	/// <summary>
	/// 比較方法を取得します。
	/// </summary>
	public BindableReactiveProperty<DisplayObject<SearchTypeComparison>?> SearchType {
		get;
	}

	/// <summary>
	/// 比較方法の選択候補一覧を取得します。
	/// </summary>
	public IReadOnlyList<DisplayObject<SearchTypeComparison>> SearchTypeList {
		get;
	}

	/// <summary>
	/// 解像度プリセットを適用するコマンドを取得します。
	/// </summary>
	public ReactiveCommand<string> ApplyPresetCommand {
		get;
	}

	/// <summary>
	/// 幅・高さの入力をクリアするコマンドを取得します。
	/// </summary>
	public ReactiveCommand ClearResolutionCommand {
		get;
	}

	/// <inheritdoc />
	public ReactiveCommand AddFilterCommand {
		get;
	}

	/// <summary>
	/// NumberBox の値（<see cref="double"/>）を解像度の整数値（未指定時は <see langword="null"/>）へ検証付きで変換します。
	/// </summary>
	/// <param name="value">変換対象の値。</param>
	/// <param name="parsed">変換後の整数値。</param>
	/// <returns>有効な入力（未指定または範囲内の整数変換可能値）である場合は <see langword="true"/>。</returns>
	public static bool TryParseDimension(double value, out int? parsed) {
		if (double.IsNaN(value)) {
			parsed = null;
			return true;
		}
		if (double.IsInfinity(value) || value < 0 || value > int.MaxValue) {
			parsed = null;
			return false;
		}
		var rounded = Math.Round(value);
		if (rounded > int.MaxValue) {
			parsed = null;
			return false;
		}
		parsed = (int)rounded;
		return true;
	}
}