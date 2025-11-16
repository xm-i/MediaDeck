using MediaDeck.Composition.Bases;
using MediaDeck.Stores.State;

namespace MediaDeck.Models.Files.Filter;
/// <summary>
/// フィルターマネージャー
/// </summary>
/// <remarks>
/// コンストラクタ
/// </remarks>
[AddSingleton]
public class FilterManager : ModelBase {
	private readonly StateStore _stateStore;

	public FilterManager(StateStore stateStore) {
		this._stateStore = stateStore;
		this.FilteringConditions = [.. stateStore.State.SearchState.FilteringConditions.Select(x => new FilteringConditionEditor(x))];
	}

	/// <summary>
	/// フィルター条件リスト
	/// </summary>
	public ObservableList<FilteringConditionEditor> FilteringConditions {
		get;
	}

	/// <summary>
	/// 保存
	/// </summary>
	public void Save() {
		this._stateStore.Save();
	}

	/// <summary>
	/// フィルタリング条件追加
	/// </summary>
	public void AddCondition() {
		var fo = this._stateStore.State.SearchState.AddFilteringCondition();
		this.FilteringConditions.Add(new FilteringConditionEditor(fo));
	}

	/// <summary>
	/// フィルタリング条件削除
	/// </summary>
	/// <param name="filteringCondition">削除するフィルタリング条件</param>
	public void RemoveCondition(FilteringConditionEditor filteringCondition) {
		this._stateStore.State.SearchState.RemoveFilteringCondition(filteringCondition.FilterObject);
		this.FilteringConditions.Remove(filteringCondition);
	}
}
