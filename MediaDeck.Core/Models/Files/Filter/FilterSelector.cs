using MediaDeck.Common.Base;
using MediaDeck.Composition.Interfaces.Notifications;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Composition.Tables;

namespace MediaDeck.Core.Models.Files.Filter;

/// <summary>
/// フィルターマネージャー
/// </summary>
[Inject(InjectServiceLifetime.Scoped)]
public class FilterSelector : ModelBase {
	/// <summary>
	/// コンストラクタ
	/// </summary>
	public FilterSelector(TabStateModel tabState, SearchDefinitionsStateModel searchDefinitions, ISearchConditionNotificationDispatcher dispatcher) {
		this._searchConditionNotificationDispatcher = dispatcher;
		this.FilteringConditions.AddRange(searchDefinitions.FilteringConditions.Select(x => new FilteringCondition(x).AddTo(this.CompositeDisposable)));

		this.CurrentFilteringConditions.Value = this.FilteringConditions
			.Where(x => tabState.SearchState.CurrentFilteringConditions.Value.Contains(x.FilterObject))
			.ToArray();

		this._innerSubscriptions.AddTo(this.CompositeDisposable);
		this.RebuildInnerSubscriptions(this.CurrentFilteringConditions.Value);

		this.CurrentFilteringConditions
			.Subscribe(selected => {
				var conditions = selected ?? [];
				this.RebuildInnerSubscriptions(conditions);
				tabState.SearchState.CurrentFilteringConditions.Value = conditions.Select(x => x.FilterObject).ToArray();
				dispatcher.FilterChanged.OnNext(Unit.Default);
			})
			.AddTo(this.CompositeDisposable);

		searchDefinitions.FilteringConditions.ObserveChanged()
			.Subscribe(_ => {
				var previousIdList = this.CurrentFilteringConditions.Value.Select(x => x.FilterObject.Id).ToHashSet();
				this.FilteringConditions.Clear();
				this.FilteringConditions.AddRange(searchDefinitions.FilteringConditions.Select(x => new FilteringCondition(x).AddTo(this.CompositeDisposable)));
				this.CurrentFilteringConditions.Value = this.FilteringConditions.Where(x => previousIdList.Contains(x.FilterObject.Id)).ToArray();
			})
			.AddTo(this.CompositeDisposable);
	}

	private readonly CompositeDisposable _innerSubscriptions = [];
	private readonly ISearchConditionNotificationDispatcher _searchConditionNotificationDispatcher;

	private void RebuildInnerSubscriptions(IReadOnlyCollection<FilteringCondition> selected) {
		this._innerSubscriptions.Clear();
		foreach (var condition in selected) {
			condition.OnUpdateFilteringConditions
				.Subscribe(_ => {
					this._searchConditionNotificationDispatcher.FilterChanged.OnNext(Unit.Default);
				})
				.AddTo(this._innerSubscriptions);
		}
	}

	/// <summary>
	/// 選択中のフィルター条件（複数選択：AND条件として適用される）
	/// </summary>
	public ReactiveProperty<FilteringCondition[]> CurrentFilteringConditions {
		get;
	} = new([]);

	/// <summary>
	/// フィルター条件リスト
	/// </summary>
	public ObservableList<FilteringCondition> FilteringConditions {
		get;
	} = [];

	/// <summary>
	/// フィルターマネージャーで選択したフィルターを引数に渡されたクエリに対しAND条件で適用して返却する。
	/// </summary>
	/// <param name="query">絞り込みクエリを適用するクエリ</param>
	/// <returns>フィルター適用後クエリ</returns>
	public IQueryable<MediaItem> SetFilterConditions(IQueryable<MediaItem> query) {
		foreach (var condition in this.CurrentFilteringConditions.Value ?? []) {
			query = condition.SetFilterConditions(query);
		}
		return query;
	}

}