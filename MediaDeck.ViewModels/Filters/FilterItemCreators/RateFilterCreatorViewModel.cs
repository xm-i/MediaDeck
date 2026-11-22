using MediaDeck.Common.Base;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Core.Models.Files.Filter.FilterItemObjects;
using MediaDeck.Core.Primitives;

namespace MediaDeck.ViewModels.Filters.FilterItemCreators;

/// <summary>
/// 評価フィルター作成ViewModel
/// </summary>
public class RateFilterCreatorViewModel : ViewModelBase, IFilterCreatorViewModel {
	private readonly IStringProvider _stringProvider;

	/// <summary>
	/// <see cref="RateFilterCreatorViewModel"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="target">追加先のフィルター条件編集 ViewModel。</param>
	/// <param name="stringProvider">文字列リソースプロバイダー。</param>
	public RateFilterCreatorViewModel(ReactiveProperty<FilteringConditionEditorViewModel?> target, IStringProvider stringProvider) {
		this._stringProvider = stringProvider;
		this.IsSelected = new BindableReactiveProperty<bool>(false).AddTo(this.CompositeDisposable);
		this.Rate = new BindableReactiveProperty<int>(3).AddTo(this.CompositeDisposable);
		this.RatingControlValue = new BindableReactiveProperty<double>(3.0).AddTo(this.CompositeDisposable);
		this.RateCaption = new BindableReactiveProperty<string>(this.FormatRateCaption(3)).AddTo(this.CompositeDisposable);
		this.SearchType = new BindableReactiveProperty<DisplayObject<SearchTypeComparison>?>().AddTo(this.CompositeDisposable);

		this.SearchTypeList = [
			new DisplayObject<SearchTypeComparison>(this._stringProvider.GetString("FilterCreator_Comparison_GreaterThan"), SearchTypeComparison.GreaterThan),
			new DisplayObject<SearchTypeComparison>(this._stringProvider.GetString("FilterCreator_Comparison_GreaterThanOrEqual"), SearchTypeComparison.GreaterThanOrEqual),
			new DisplayObject<SearchTypeComparison>(this._stringProvider.GetString("FilterCreator_Comparison_Equal"), SearchTypeComparison.Equal),
			new DisplayObject<SearchTypeComparison>(this._stringProvider.GetString("FilterCreator_Comparison_LessThanOrEqual"), SearchTypeComparison.LessThanOrEqual),
			new DisplayObject<SearchTypeComparison>(this._stringProvider.GetString("FilterCreator_Comparison_LessThan"), SearchTypeComparison.LessThan)
		];
		this.SearchType.Value = this.SearchTypeList.First(x => x.Value == SearchTypeComparison.GreaterThanOrEqual);

		this.Rate.Subscribe(rate => {
			var clamped = Math.Clamp(rate, 0, 5);
			var targetControlVal = clamped <= 0 ? -1.0 : (double)clamped;
			if (Math.Abs(this.RatingControlValue.Value - targetControlVal) > 0.001) {
				this.RatingControlValue.Value = targetControlVal;
			}
			this.RateCaption.Value = this.FormatRateCaption(clamped);
		}).AddTo(this.CompositeDisposable);

		this.ClearRateCommand = new ReactiveCommand().AddTo(this.CompositeDisposable);
		this.ClearRateCommand.Subscribe(_ => {
			this.Rate.Value = 0;
			this.RatingControlValue.Value = -1.0;
		}).AddTo(this.CompositeDisposable);

		this.AddFilterCommand = this.Rate
			.CombineLatest(this.SearchType, (rate, st) => rate >= 0 && rate <= 5 && st is not null)
			.ToReactiveCommand()
			.AddTo(this.CompositeDisposable);

		this.AddFilterCommand.Subscribe(_ => {
			if (this.SearchType.Value is not { } searchType) {
				return;
			}
			var filter = new RateFilterItemObject {
				Rate = Math.Clamp(this.Rate.Value, 0, 5),
				SearchType = searchType.Value
			};
			target.Value?.AddFilter(filter);
		}).AddTo(this.CompositeDisposable);
	}

	/// <inheritdoc />
	public FilterCreatorKind Kind {
		get {
			return FilterCreatorKind.Rate;
		}
	}

	/// <inheritdoc />
	public string Title {
		get {
			return this._stringProvider.GetString("FilterCreator_Rate_Title");
		}
	}

	/// <inheritdoc />
	public string ShortTitle {
		get {
			return this._stringProvider.GetString("FilterCreator_Rate_ShortTitle");
		}
	}

	/// <inheritdoc />
	public string IconGlyph {
		get {
			return "\uE734";
		}
	}

	/// <inheritdoc />
	public string Description {
		get {
			return this._stringProvider.GetString("FilterCreator_Rate_Description");
		}
	}

	/// <inheritdoc />
	public BindableReactiveProperty<bool> IsSelected {
		get;
	}

	/// <summary>
	/// 設定された評価値（0〜5）を取得します。
	/// </summary>
	public BindableReactiveProperty<int> Rate {
		get;
	}

	/// <summary>
	/// RatingControl 表示用の評価値（0のときは -1.0）を取得します。
	/// </summary>
	public BindableReactiveProperty<double> RatingControlValue {
		get;
	}

	/// <summary>
	/// 現在の評価値を表すラベル文字列を取得します。
	/// </summary>
	public BindableReactiveProperty<string> RateCaption {
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
	/// 評価値を 0（未評価）にリセットするコマンドを取得します。
	/// </summary>
	public ReactiveCommand ClearRateCommand {
		get;
	}

	/// <inheritdoc />
	public ReactiveCommand AddFilterCommand {
		get;
	}

	/// <summary>
	/// RatingControl からの値変更を評価値（0〜5）へ反映します。
	/// </summary>
	/// <param name="value">RatingControl の値。</param>
	public void UpdateRateFromRatingControl(double value) {
		if (double.IsNaN(value) || value < 0) {
			this.Rate.Value = 0;
			return;
		}
		this.Rate.Value = Math.Clamp((int)Math.Round(value), 0, 5);
	}

	private string FormatRateCaption(int rate) {
		return rate <= 0
			? this._stringProvider.GetString("FilterCreator_Rate_Unrated")
			: this._stringProvider.GetString("FilterCreator_Rate_Stars", rate);
	}
}