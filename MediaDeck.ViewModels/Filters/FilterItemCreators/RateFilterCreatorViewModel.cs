using System.ComponentModel.DataAnnotations;

using MediaDeck.Common.Base;
using MediaDeck.Common.Extensions;
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
	/// 表示名
	/// </summary>
	public string Title {
		get {
			return this._stringProvider.GetString("FilterCreator_Rate_Title");
		}
	}

	/// <summary>
	/// 評価 チェック用テキスト
	/// </summary>
	[Range(0, 5)]
	public BindableReactiveProperty<string?> RateText {
		get;
	}

	/// <summary>
	/// 検索条件として指定のタグを含むものを検索するか、含まないものを検索するかを選択する。
	/// </summary>
	public BindableReactiveProperty<DisplayObject<SearchTypeComparison>> SearchType {
		get;
	} = new();

	/// <summary>
	/// 含む/含まないの選択候補
	/// </summary>
	public IEnumerable<DisplayObject<SearchTypeComparison>> SearchTypeList {
		get;
	}

	/// <summary>
	/// フィルター追加コマンド
	/// </summary>
	public ReactiveCommand AddFilterCommand {
		get;
	}

	public RateFilterCreatorViewModel(ReactiveProperty<FilteringConditionEditorViewModel?> target, IStringProvider stringProvider) {
		this._stringProvider = stringProvider;
		this.SearchTypeList = [
			new DisplayObject<SearchTypeComparison>(this._stringProvider.GetString("FilterCreator_Comparison_GreaterThan"), SearchTypeComparison.GreaterThan),
			new DisplayObject<SearchTypeComparison>(this._stringProvider.GetString("FilterCreator_Comparison_GreaterThanOrEqual"), SearchTypeComparison.GreaterThanOrEqual),
			new DisplayObject<SearchTypeComparison>(this._stringProvider.GetString("FilterCreator_Comparison_Equal"), SearchTypeComparison.Equal),
			new DisplayObject<SearchTypeComparison>(this._stringProvider.GetString("FilterCreator_Comparison_LessThanOrEqual"), SearchTypeComparison.LessThanOrEqual),
			new DisplayObject<SearchTypeComparison>(this._stringProvider.GetString("FilterCreator_Comparison_LessThan"), SearchTypeComparison.LessThan)
		];
		this.RateText = new BindableReactiveProperty<string?>().EnableValidation(() => this.RateText).AddTo(this.CompositeDisposable);
		this.SearchType.Value = this.SearchTypeList.First(x => x.Value == SearchTypeComparison.GreaterThanOrEqual);
		this.AddFilterCommand =
			this.RateText.Select(string.IsNullOrEmpty)
				.CombineLatest(this.RateText.ErrorsChangedAsObservable().Select(_ => this.RateText.HasErrors), (x, x2) => !x && !x2)
				.ToReactiveCommand()
				.AddTo(this.CompositeDisposable);

		this.AddFilterCommand
			.Subscribe(vm => {
				if (int.TryParse(this.RateText.Value, out var r)) {
					var filter = new RateFilterItemObject {
						Rate = r,
						SearchType = this.SearchType.Value.Value
					};
					target.Value?.AddFilter(filter);
				}
			}).AddTo(this.CompositeDisposable);
	}
}