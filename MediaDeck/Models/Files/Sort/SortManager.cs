using MediaDeck.Composition.Bases;
using MediaDeck.Models.Preferences;

namespace MediaDeck.Models.Files.Sort;
/// <summary>
/// ソートマネージャー
/// </summary>
[AddSingleton]
public class SortManager(States states) : ModelBase {
	private readonly States _states = states;
	/// <summary>
	/// ソート条件リスト
	/// </summary>
	public ObservableList<SortConditionEditor> SortConditions {
		get;
	} = [];

	/// <summary>
	/// 読み込み
	/// </summary>
	public void Load() {
		this.SortConditions.Clear();
		this.SortConditions.AddRange(this._states.SearchStates.SortConditions.Select(x => new SortConditionEditor(x)));
	}

	/// <summary>
	/// 保存
	/// </summary>
	public void Save() {
		// 削除分
		this._states.SearchStates.SortConditions.RemoveRange(this._states.SearchStates.SortConditions.Except(this.SortConditions.Select(x => x.SortObject)));
		// 追加分
		this._states.SearchStates.SortConditions.AddRange(this.SortConditions.Select(x => x.SortObject).Except(this._states.SearchStates.SortConditions));
		// 更新分
		foreach (var sortCondition in this.SortConditions) {
			sortCondition.Save();
		}
		this._states.Save();
	}

	/// <summary>
	/// ソート条件追加
	/// </summary>
	public void AddCondition() {
		this.SortConditions.Add(new SortConditionEditor(new SortObject()));
	}

	/// <summary>
	/// ソート条件削除
	/// </summary>
	/// <param name="sortCondition">削除するソート条件</param>
	public void RemoveCondition(SortConditionEditor sortCondition) {
		this.SortConditions.Remove(sortCondition);
	}
}