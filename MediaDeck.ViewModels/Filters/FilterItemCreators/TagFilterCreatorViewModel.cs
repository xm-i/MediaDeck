using MediaDeck.Common.Base;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Core.Models.Files.Filter.FilterItemObjects;
using MediaDeck.Core.Primitives;

namespace MediaDeck.ViewModels.Filters.FilterItemCreators;

/// <summary>
/// タグフィルター作成ViewModel
/// </summary>
public class TagFilterCreatorViewModel : ViewModelBase, IFilterCreatorViewModel {
	private readonly IStringProvider _stringProvider;

	/// <summary>
	/// 表示名
	/// </summary>
	public string Title {
		get {
			return this._stringProvider.GetString("FilterCreator_Tag_Title");
		}
	}

	/// <summary>
	/// タグ名
	/// </summary>
	public BindableReactiveProperty<string> TagName {
		get;
	}

	/// <summary>
	/// 検索条件として指定のタグを含むものを検索するか、含まないものを検索するかを選択する。
	/// </summary>
	public BindableReactiveProperty<DisplayObject<SearchTypeInclude>> SearchType {
		get;
	}

	/// <summary>
	/// 含む/含まないの選択候補
	/// </summary>
	public IEnumerable<DisplayObject<SearchTypeInclude>> SearchTypeList {
		get;
	}

	/// <summary>
	/// フィルター追加コマンド
	/// </summary>
	public ReactiveCommand AddFilterCommand {
		get;
	}

	/// <summary>
	/// <see cref="TagFilterCreatorViewModel"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="target">追加先のフィルター条件編集 ViewModel</param>
	/// <param name="stringProvider">文字列リソースプロバイダー</param>
	public TagFilterCreatorViewModel(ReactiveProperty<FilteringConditionEditorViewModel?> target, IStringProvider stringProvider) {
		this._stringProvider = stringProvider;
		this.TagName = new BindableReactiveProperty<string>(string.Empty).AddTo(this.CompositeDisposable);
		this.SearchType = new BindableReactiveProperty<DisplayObject<SearchTypeInclude>>().AddTo(this.CompositeDisposable);
		this.SearchTypeList = [
			new DisplayObject<SearchTypeInclude>(this._stringProvider.GetString("FilterCreator_Include"), SearchTypeInclude.Include),
			new DisplayObject<SearchTypeInclude>(this._stringProvider.GetString("FilterCreator_Exclude"), SearchTypeInclude.Exclude)
		];
		this.SearchType.Value = this.SearchTypeList.First();
		this.AddFilterCommand = this.TagName.Select(x => !string.IsNullOrEmpty(x)).ToReactiveCommand().AddTo(this.CompositeDisposable);
		this.AddFilterCommand.Subscribe(_ => {
			var filter = new TagFilterItemObject {
				TagName = this.TagName.Value,
				SearchType = this.SearchType.Value.Value
			};
			target.Value?.AddFilter(filter);
		})
			.AddTo(this.CompositeDisposable);
	}
}