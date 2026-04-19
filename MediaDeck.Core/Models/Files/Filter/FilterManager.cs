using MediaDeck.Common.Base;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Core.Stores.State;

namespace MediaDeck.Core.Models.Files.Filter;

/// <summary>
/// フィルターマネージャー
/// </summary>
/// <remarks>
/// コンストラクタ
/// </remarks>
[Inject(InjectServiceLifetime.Singleton)]
public class FilterManager : ModelBase {
	private readonly IStateStore _stateStore;
	private readonly SearchDefinitionsStateModel _searchDefinitions;

	public FilterManager(IStateStore stateStore, SearchDefinitionsStateModel searchDefinitions) {
		this._stateStore = stateStore;
		this._searchDefinitions = searchDefinitions;
		this.FilteringConditions = [.. searchDefinitions.FilteringConditions.Select(x => new FilteringConditionEditor(x))];
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
		var fo = this._searchDefinitions.AddFilteringCondition();
		this.FilteringConditions.Add(new FilteringConditionEditor(fo));
	}

	/// <summary>
	/// フィルタリング条件削除
	/// </summary>
	/// <param name="filteringCondition">削除するフィルタリング条件</param>
	public void RemoveCondition(FilteringConditionEditor filteringCondition) {
		this._searchDefinitions.RemoveFilteringCondition(filteringCondition.FilterObject);
		this.FilteringConditions.Remove(filteringCondition);
	}
}