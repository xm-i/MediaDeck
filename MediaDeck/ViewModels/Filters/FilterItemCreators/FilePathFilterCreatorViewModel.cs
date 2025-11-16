using System.Collections.Generic;

using MediaDeck.Composition.Bases;
using MediaDeck.Composition.Enum;
using MediaDeck.Models.Files.Filter.FilterItemObjects;
using MediaDeck.Utils.Objects;

namespace MediaDeck.ViewModels.Filters.FilterItemCreators;
/// <summary>
/// ファイルパスフィルター作成ViewModel
/// </summary>
public class FilePathFilterCreatorViewModel : ViewModelBase, IFilterCreatorViewModel {
	/// <summary>
	/// 表示名
	/// </summary>
	public string Title {
		get {
			return "ファイルパスフィルター";
		}

	}

	/// <summary>
	/// タグ名
	/// </summary>
	public BindableReactiveProperty<string> FilePath {
		get;
	} = new();

	/// <summary>
	/// 検索条件として指定のタグを含むものを検索するか、含まないものを検索するかを選択する。
	/// </summary>
	public BindableReactiveProperty<DisplayObject<SearchTypeInclude>> SearchType {
		get;
	} = new();

	/// <summary>
	/// 含む/含まないの選択候補
	/// </summary>
	public IEnumerable<DisplayObject<SearchTypeInclude>> SearchTypeList {
		get;
	} = [
		new DisplayObject<SearchTypeInclude>("含む",SearchTypeInclude.Include),
		new DisplayObject<SearchTypeInclude>("含まない",SearchTypeInclude.Exclude)
	];

	/// <summary>
	/// フィルター追加コマンド
	/// </summary>
	public ReactiveCommand AddFilterCommand {
		get;
	}

	public FilePathFilterCreatorViewModel(ReactiveProperty<FilteringConditionEditorViewModel?> target) {
		this.SearchType.Value = this.SearchTypeList.First();
		this.AddFilterCommand = this.FilePath.Select(x => !string.IsNullOrEmpty(x)).ToReactiveCommand();
		this.AddFilterCommand.Subscribe(vm => {
			var filter = new FilePathFilterItemObject(this.FilePath.Value, this.SearchType.Value.Value);
			target.Value?.AddFilter(filter);
		}).AddTo(this.CompositeDisposable);
	}
}
