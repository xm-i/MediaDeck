using System.ComponentModel;

using MediaDeck.Composition.Interfaces.Files;

using R3.JsonConfig.Attributes;

namespace MediaDeck.Composition.Stores.State.Model;

/// <summary>
/// タブ固有の検索選択状態
/// </summary>
[Inject(InjectServiceLifetime.Transient)]
[GenerateR3JsonConfigDto]
public class SearchStateModel {
	/// <summary>
	/// カレント検索条件
	/// </summary>
	public ObservableList<ISearchCondition> SearchCondition {
		get;
	} = [];

	/// <summary>
	/// カレントフィルター条件
	/// </summary>
	public ReactiveProperty<Guid?> CurrentFilteringCondition {
		get;
	} = new(null);

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
}