using MediaDeck.Common.Base;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Composition.Stores.State.Model.Objects;
using MediaDeck.Core.Stores.State;

namespace MediaDeck.Core.Models.Files.Sort;

/// <summary>
/// ソートマネージャー
/// </summary>
[Inject(InjectServiceLifetime.Singleton)]
public class SortManager : ModelBase {
	private readonly IStateStore _stateStore;
	private readonly SearchDefinitionsStateModel _searchDefinitions;

	public SortManager(IStateStore stateStore, SearchDefinitionsStateModel searchDefinitions) {
		this._stateStore = stateStore;
		this._searchDefinitions = searchDefinitions;
		this.SortConditions = searchDefinitions.SortConditions;
	}

	/// <summary>
	/// ソート条件リスト
	/// </summary>
	public ObservableList<SortObject> SortConditions {
		get;
	}

	/// <summary>
	/// 保存
	/// </summary>
	public void Save() {
		this._stateStore.Save();
	}

	/// <summary>
	/// ソート条件追加
	/// </summary>
	public void AddCondition() {
		var so = this._searchDefinitions.AddSortCondition();
	}

	/// <summary>
	/// ソート条件削除
	/// </summary>
	/// <param name="sortObject">削除するソート条件</param>
	public void RemoveCondition(SortObject sortObject) {
		this._searchDefinitions.RemoveSortCondition(sortObject);
	}
}