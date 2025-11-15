using MediaDeck.Composition.Bases;
using MediaDeck.Models.Files.Filter;
using MediaDeck.Models.Files.Filter.FilterItemObjects;

namespace MediaDeck.ViewModels.Panes.FilterPanes;

public class FilteringConditionViewModel : ViewModelBase {
	/// <summary>
	/// モデル
	/// </summary>
	public FilteringCondition Model {
		get;
	}

	/// <summary>
	/// 表示名
	/// </summary>
	public BindableReactiveProperty<string> DisplayName {
		get;
	}

	/// <summary>
	/// フィルター条件クリエイター
	/// </summary>
	public INotifyCollectionChangedSynchronizedViewList<IFilterItemObject> FilterItems {
		get;
	}

	public FilteringConditionViewModel(FilteringCondition model) {
		this.Model = model;
		this.DisplayName = this.Model.DisplayName.ToBindableReactiveProperty(null!);

		this.FilterItems = this.Model.FilterItemObjects.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);
	}
}
