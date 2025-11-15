using System.Collections.Generic;

using MediaDeck.Composition.Bases;
using MediaDeck.Models.Files.Filter.FilterItemObjects;
using MediaDeck.Utils.Objects;

namespace MediaDeck.ViewModels.Filters.FilterItemCreators;
/// <summary>
/// メディアタイプフィルター作成ViewModel
/// </summary>
public class MediaTypeFilterCreatorViewModel : ViewModelBase, IFilterCreatorViewModel {
	/// <summary>
	/// 表示名
	/// </summary>
	public string Title {
		get {
			return "メディアタイプフィルター";
		}
	}

	/// <summary>
	/// フィルター追加コマンド
	/// </summary>
	public ReactiveCommand AddFilterCommand {
		get;
	} = new();

	/// <summary>
	/// メディアタイプ
	/// </summary>
	public BindableReactiveProperty<DisplayObject<bool>> MediaType {
		get;
	} = new();

	/// <summary>
	/// メディアタイプ候補
	/// </summary>
	public IEnumerable<DisplayObject<bool>> MediaTypeList {
		get;
	} = [
		new DisplayObject<bool>("画像",false),
		new DisplayObject<bool>("動画",true)
	];

	public MediaTypeFilterCreatorViewModel(ReactiveProperty<FilteringConditionEditorViewModel?> target) {
		this.MediaType.Value = this.MediaTypeList.First();
		this.AddFilterCommand.Subscribe(vm => {
			var filter = new MediaTypeFilterItemObject(this.MediaType.Value.Value);
			target.Value?.AddFilter(filter);
		});

	}
}
