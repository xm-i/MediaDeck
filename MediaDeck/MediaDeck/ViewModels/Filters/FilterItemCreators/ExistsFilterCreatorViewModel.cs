using System.Collections.Generic;

using MediaDeck.Composition.Bases;
using MediaDeck.Models.Files.Filter.FilterItemObjects;
using MediaDeck.Utils.Objects;

namespace MediaDeck.ViewModels.Filters.FilterItemCreators;
/// <summary>
/// 存在フィルター作成ViewModel
/// </summary>
public class ExistsFilterCreatorViewModel : ViewModelBase, IFilterCreatorViewModel {
	/// <summary>
	/// 表示名
	/// </summary>
	public string Title {
		get {
			return "存在フィルター";
		}
	}

	/// <summary>
	/// ファイルが存在するか否か
	/// </summary>
	public BindableReactiveProperty<DisplayObject<bool>> Exists {
		get;
	} = new();

	/// <summary>
	/// ファイルが存在するか否かの候補
	/// </summary>
	public IEnumerable<DisplayObject<bool>> ExistsList {
		get;
	} = [
		new DisplayObject<bool>("ファイルが存在する",true),
		new DisplayObject<bool>("ファイルが存在しない",false)
	];

	/// <summary>
	/// フィルター追加コマンド
	/// </summary>
	public ReactiveCommand AddFilterCommand {
		get;
	} = new();

	public ExistsFilterCreatorViewModel(ReactiveProperty<FilteringConditionEditorViewModel?> target) {
		this.Exists.Value = this.ExistsList.First();
		this.AddFilterCommand.Subscribe(vm => {
			var filter = new ExistsFilterItemObject(this.Exists.Value.Value);
			target.Value?.AddFilter(filter);
		});
	}
}
