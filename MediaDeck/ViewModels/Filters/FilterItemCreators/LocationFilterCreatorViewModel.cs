using System.Collections.Generic;

using MediaDeck.Composition.Bases;
using MediaDeck.Models.Files.Filter.FilterItemObjects;
using MediaDeck.Utils.Objects;

namespace MediaDeck.ViewModels.Filters.FilterItemCreators;
/// <summary>
/// 座標フィルター作成ViewModel
/// </summary>
public class LocationFilterCreatorViewModel : ViewModelBase, IFilterCreatorViewModel {
	/// <summary>
	/// 表示名
	/// </summary>
	public string Title {
		get {
			return "座標フィルター";
		}
	}

	/// <summary>
	/// フィルター追加コマンド
	/// </summary>
	public ReactiveCommand AddFilterCommand {
		get;
	} = new();

	/// <summary>
	/// 座標情報を持っているか否か
	/// </summary>
	public BindableReactiveProperty<DisplayObject<bool>> HasLocation {
		get;
	} = new();

	/// <summary>
	/// 座標情報を持っているか否かの候補
	/// </summary>
	public IEnumerable<DisplayObject<bool>> HasLocationList {
		get;
	} = [
		new DisplayObject<bool>("座標情報を含む",true),
		new DisplayObject<bool>("座標情報を含まない",false)
	];

	public LocationFilterCreatorViewModel(ReactiveProperty<FilteringConditionEditorViewModel?> target) {
		this.HasLocation.Value = this.HasLocationList.First();
		this.AddFilterCommand.Subscribe(vm => {
			var filter = new LocationFilterItemObject(this.HasLocation.Value.Value);
			target.Value?.AddFilter(filter);
		});
	}
}
