using System.Collections.Generic;

using MediaDeck.Composition.Bases;
using MediaDeck.Composition.Stores.State.Model.Objects;
using MediaDeck.Stores.State;

namespace MediaDeck.Models.Files.Filter;
/// <summary>
/// フィルターマネージャー
/// </summary>
/// <remarks>
/// コンストラクタ
/// </remarks>
[AddSingleton]
public class FilterManager(StateStore stateStore) : ModelBase {
	private readonly StateStore _stateStore = stateStore;
	/// <summary>
	/// フィルター条件リスト
	/// </summary>
	public ObservableList<FilteringConditionEditor> FilteringConditions {
		get;
	} = [];

	/// <summary>
	/// 読み込み
	/// </summary>
	public void Load() {
		this.FilteringConditions.Clear();
		this.FilteringConditions.AddRange(this._stateStore.State.SearchState.FilteringConditions.Select(x => new FilteringConditionEditor(x)));
	}

	/// <summary>
	/// 保存
	/// </summary>
	public void Save() {
		// 削除分
		this._stateStore.State.SearchState.FilteringConditions.RemoveRange(this._stateStore.State.SearchState.FilteringConditions.Except(this.FilteringConditions.Select(x => x.FilterObject)));
		// 追加分
		this._stateStore.State.SearchState.FilteringConditions.AddRange(this.FilteringConditions.Select(x => x.FilterObject).Except(this._stateStore.State.SearchState.FilteringConditions));
		// 更新分
		foreach (var filteringCondition in this.FilteringConditions) {
			filteringCondition.Save();
		}
		this._stateStore.Save();
	}

	/// <summary>
	/// フィルタリング条件追加
	/// </summary>
	public void AddCondition() {
		this.FilteringConditions.Add(new FilteringConditionEditor(new FilterObject()));
	}

	/// <summary>
	/// フィルタリング条件削除
	/// </summary>
	/// <param name="filteringCondition">削除するフィルタリング条件</param>
	public void RemoveCondition(FilteringConditionEditor filteringCondition) {
		this.FilteringConditions.Remove(filteringCondition);
	}
}
