using MediaDeck.Common.Base;
using MediaDeck.Composition.Enum;
using MediaDeck.Core.Models.Files.Filter.FilterItemObjects;
using MediaDeck.Core.Primitives;

namespace MediaDeck.ViewModels.Filters.FilterItemCreators;

/// <summary>
/// フォルダーグループフィルター作成ViewModel
/// </summary>
public class FolderGroupFilterCreatorViewModel : ViewModelBase, IFilterCreatorViewModel {
	/// <summary>
	/// 表示名
	/// </summary>
	public string Title {
		get {
			return "フォルダグループフィルター";
		}
	}

	/// <summary>
	/// タグ名
	/// </summary>
	public BindableReactiveProperty<string> TagName {
		get;
	} = new();

	/// <summary>
	/// 検索条件としてフォルダ配下のファイルを検索するか、フォルダ配下外のファイルを検索するかを選択する。
	/// </summary>
	public BindableReactiveProperty<DisplayObject<SearchTypeInclude>> SearchType {
		get;
	} = new();

	/// <summary>
	/// フォルダ配下/フォルダ配下外の選択候補
	/// </summary>
	public IEnumerable<DisplayObject<SearchTypeInclude>> SearchTypeList {
		get;
	} = [
		new DisplayObject<SearchTypeInclude>("配下", SearchTypeInclude.Include),
		new DisplayObject<SearchTypeInclude>("配下外", SearchTypeInclude.Exclude)
	];

	/// <summary>
	/// フィルター追加コマンド
	/// </summary>
	public ReactiveCommand AddFilterCommand {
		get;
	} = new();

	public FolderGroupFilterCreatorViewModel(ReactiveProperty<FilteringConditionEditorViewModel?> target) {
		this.SearchType.Value = this.SearchTypeList.First();
		this.AddFilterCommand.Subscribe(_ => {
			var filter = new FolderGroupFilterItemObject {
				SearchType = this.SearchType.Value.Value
			};
			target.Value?.AddFilter(filter);
		}).AddTo(this.CompositeDisposable);
	}
}