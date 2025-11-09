using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reactive.Linq;

using MediaDeck.Composition.Bases;
using MediaDeck.Models.Files.Filter.FilterItemObjects;
using MediaDeck.Utils.Enums;
using MediaDeck.Utils.Objects;

using Reactive.Bindings.Extensions;

namespace MediaDeck.ViewModels.Filters.FilterItemCreators;
/// <summary>
/// 評価フィルター作成ViewModel
/// </summary>
public class RateFilterCreatorViewModel : ViewModelBase, IFilterCreatorViewModel {
	/// <summary>
	/// 表示名
	/// </summary>
	public string Title {
		get {
			return "評価フィルター";
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
	} = [
		new DisplayObject<SearchTypeComparison>("を超える",SearchTypeComparison.GreaterThan),
		new DisplayObject<SearchTypeComparison>("以上",SearchTypeComparison.GreaterThanOrEqual),
		new DisplayObject<SearchTypeComparison>("と等しい",SearchTypeComparison.Equal),
		new DisplayObject<SearchTypeComparison>("以下",SearchTypeComparison.LessThanOrEqual),
		new DisplayObject<SearchTypeComparison>("未満",SearchTypeComparison.LessThan)
	];

	/// <summary>
	/// フィルター追加コマンド
	/// </summary>
	public ReactiveCommand AddFilterCommand {
		get;
	}

	public RateFilterCreatorViewModel(ReactiveProperty<FilteringConditionEditorViewModel?> target) {
		this.RateText = new ReactiveProperty<string?>().ToBindableReactiveProperty().EnableValidation(() => this.RateText);
		this.SearchType.Value = this.SearchTypeList.First(x => x.Value == SearchTypeComparison.GreaterThanOrEqual);
		this.AddFilterCommand =
			this.RateText.Select(string.IsNullOrEmpty)
				.CombineLatest(this.RateText.ErrorsChangedAsObservable().Select(_ => this.RateText.HasErrors).ToObservable(), (x, x2) => !x && !x2)
				.ToReactiveCommand();

		this.AddFilterCommand
			.Subscribe(vm => {
				if (int.TryParse(this.RateText.Value, out var r)) {
					var filter = new RateFilterItemObject(r, this.SearchType.Value.Value);
					target.Value?.AddFilter(filter);
				}
			});

	}
}
