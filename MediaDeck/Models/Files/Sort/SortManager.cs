using MediaDeck.Composition.Bases;
using MediaDeck.Composition.Stores.State.Model.Objects;
using MediaDeck.Stores.State;

namespace MediaDeck.Models.Files.Sort;
/// <summary>
/// ソートマネージャー
/// </summary>
[AddSingleton]
public class SortManager : ModelBase {
	private readonly StateStore _stateStore;

	public SortManager(StateStore stateStore) {
		this._stateStore = stateStore;
		this.SortConditions = this._stateStore.State.SearchState.SortConditions;
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
		var so = this._stateStore.State.SearchState.AddSortCondition();
	}

	/// <summary>
	/// ソート条件削除
	/// </summary>
	/// <param name="sortObject">削除するソート条件</param>
	public void RemoveCondition(SortObject sortObject) {
		this._stateStore.State.SearchState.RemoveSortCondition(sortObject);
	}
}