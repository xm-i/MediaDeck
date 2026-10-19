using System.ComponentModel;
using GenJsonConfig.Attributes;
using MediaDeck.Composition.Interfaces.Files;

namespace MediaDeck.Composition.Stores.State.Model;

/// <summary>
/// タブ固有の検索選択状態
/// </summary>
[Inject(InjectServiceLifetime.Transient)]
[GenerateJsonConfigDto]
public class SearchStateModel {
	/// <summary>
	/// カレント検索条件
	/// </summary>
	public ObservableList<ISearchCondition> SearchCondition {
		get;
	} = [];

	/// <summary>
	/// カレントフィルター条件（複数選択：AND条件として適用される）
	/// </summary>
	public ReactiveProperty<Guid[]> CurrentFilteringConditions {
		get;
	} = new([]);

	/// <summary>
	/// カレントソート条件
	/// </summary>
	public ReactiveProperty<Guid?> CurrentSortCondition {
		get;
	} = new(null);

	/// <summary>
	/// 全体ソート方向
	/// </summary>
	public ReactiveProperty<ListSortDirection> SortDirection {
		get;
	} = new(ListSortDirection.Ascending);

	/// <summary>
	/// 別の <see cref="SearchStateModel"/> から設定値をコピーします。
	/// </summary>
	/// <param name="source">コピー元のモデル</param>
	public void CopyFrom(SearchStateModel source) {
		this.CurrentSortCondition.Value = source.CurrentSortCondition.Value;
		this.SortDirection.Value = source.SortDirection.Value;
		this.CurrentFilteringConditions.Value = [.. source.CurrentFilteringConditions.Value];
	}
}