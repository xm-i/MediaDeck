using System.ComponentModel.DataAnnotations;

using MediaDeck.Common.Base;
using MediaDeck.Common.Extensions;
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
	/// 表示名
	/// </summary>
	public string Title {
		get {
			return this._stringProvider.GetString("FilterCreator_Resolution_Title");
		}
	}

	/// <summary>
	/// フィルター追加コマンド
	/// </summary>
	public ReactiveCommand AddFilterCommand {
		get;
	}

	/// <summary>
	/// 解像度幅
	/// </summary>
	[Range(0d, int.MaxValue)]
	public BindableReactiveProperty<string?> ResolutionWidthText {
		get;
	}

	/// <summary>
	/// 解像度高さ
	/// </summary>
	[Range(0d, int.MaxValue)]
	public BindableReactiveProperty<string?> ResolutionHeightText {
		get;
	}

	/// <summary>
	/// 検索タイプを選択
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

	public ResolutionFilterCreatorViewModel(ReactiveProperty<FilteringConditionEditorViewModel?> target, IStringProvider stringProvider) {
		this._stringProvider = stringProvider;
		this.SearchTypeList = [
			new DisplayObject<SearchTypeComparison>(this._stringProvider.GetString("FilterCreator_Comparison_GreaterThan"), SearchTypeComparison.GreaterThan),
			new DisplayObject<SearchTypeComparison>(this._stringProvider.GetString("FilterCreator_Comparison_GreaterThanOrEqual"), SearchTypeComparison.GreaterThanOrEqual),
			new DisplayObject<SearchTypeComparison>(this._stringProvider.GetString("FilterCreator_Comparison_Equal"), SearchTypeComparison.Equal),
			new DisplayObject<SearchTypeComparison>(this._stringProvider.GetString("FilterCreator_Comparison_LessThanOrEqual"), SearchTypeComparison.LessThanOrEqual),
			new DisplayObject<SearchTypeComparison>(this._stringProvider.GetString("FilterCreator_Comparison_LessThan"), SearchTypeComparison.LessThan)
		];
		this.ResolutionWidthText = new BindableReactiveProperty<string?>().EnableValidation(() => this.ResolutionWidthText).AddTo(this.CompositeDisposable);
		this.ResolutionHeightText = new BindableReactiveProperty<string?>().EnableValidation(() => this.ResolutionHeightText).AddTo(this.CompositeDisposable);
		this.SearchType.Value = this.SearchTypeList.First(x => x.Value == SearchTypeComparison.GreaterThanOrEqual);

		this.AddFilterCommand =
			this.ResolutionWidthText.Select(string.IsNullOrEmpty)
				.CombineLatest(this.ResolutionHeightText.Select(string.IsNullOrEmpty),
					this.ResolutionWidthText.ErrorsChangedAsObservable().Select(x => this.ResolutionWidthText.HasErrors),
					this.ResolutionHeightText.ErrorsChangedAsObservable().Select(_ => this.ResolutionHeightText.HasErrors),
					(x, x2, x3, x4) => !x && !x2 && !x3 && !x4)
				.ToReactiveCommand()
				.AddTo(this.CompositeDisposable);

		this.AddFilterCommand
			.Subscribe(vm => {
				int? width = null;
				int? height = null;
				if (int.TryParse(this.ResolutionWidthText.Value, out var w)) {
					width = w;
				}
				if (int.TryParse(this.ResolutionHeightText.Value, out var h)) {
					height = h;
				}

				IFilterItemObject filterItemObject;
				if (width is { } w2 && height is { } h2) {
					filterItemObject = new ResolutionFilterItemObject {
						Resolution = new ComparableSize(w2, h2),
						SearchType = this.SearchType.Value.Value
					};
				} else {
					filterItemObject = new ResolutionFilterItemObject {
						Width = width,
						Height = height,
						SearchType = this.SearchType.Value.Value
					};
				}
				target.Value?.AddFilter(filterItemObject);
			}).AddTo(this.CompositeDisposable);
	}
}