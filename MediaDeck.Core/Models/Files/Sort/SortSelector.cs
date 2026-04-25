using System.ComponentModel;

using MediaDeck.Common.Base;
using MediaDeck.Common.Extensions;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Composition.Stores.State.Model.Objects;
using MediaDeck.Core.Models.NotificationDispatcher;
using MediaDeck.Database.Tables;

namespace MediaDeck.Core.Models.Files.Sort;

[Inject(InjectServiceLifetime.Scoped)]
public class SortSelector : ModelBase {
	/// <summary>
	/// コンストラクタ
	/// </summary>
	public SortSelector(TabStateModel tabState, SearchDefinitionsStateModel searchDefinitions, SearchConditionNotificationDispatcher dispatcher) {
		// 設定値初回値読み込み
		this.SortConditions = searchDefinitions.SortConditions;
		this.CurrentSortCondition = tabState.SearchState.CurrentSortCondition.ToTwoWayReactiveProperty(x => this.SortConditions.FirstOrDefault(sc => sc.Id == x), x => x?.Id, null, this.CompositeDisposable);
		this.Direction = tabState.SearchState.SortDirection;

		// ソート条件変更を内部 Subject に通知しつつ、Dispatcher 経由で検索発火を依頼する
		this.CurrentSortCondition.Select(_ => Unit.Default)
			.Merge(this.Direction.Select(_ => Unit.Default))
			.Subscribe(_ => {
				this._onUpdateSortConditionChanged.OnNext(Unit.Default);
				dispatcher.SortChanged.OnNext(Unit.Default);
			})
			.AddTo(this.CompositeDisposable);

		IDisposable? before = null;
		this.CurrentSortCondition.Subscribe(x => {
			before?.Dispose();
			before = x?.SortItemObjects.ObserveChanged().Subscribe(_ => {
				this._onUpdateSortConditionChanged.OnNext(Unit.Default);
				dispatcher.SortChanged.OnNext(Unit.Default);
			});
		}).AddTo(this.CompositeDisposable);
	}

	/// <summary>
	/// カレントソート条件
	/// </summary>
	public ReactiveProperty<SortObject?> CurrentSortCondition {
		get;
	}

	/// <summary>
	/// ソート条件リスト
	/// </summary>
	public ObservableList<SortObject> SortConditions {
		get;
	}

	/// <summary>
	/// ソート方向
	/// </summary>
	public ReactiveProperty<ListSortDirection> Direction {
		get;
	}

	/// <summary>
	/// ソート条件変更通知Subject
	/// </summary>
	private readonly Subject<Unit> _onUpdateSortConditionChanged = new();

	/// <summary>
	/// フィルター条件変更通知
	/// </summary>
	public Observable<Unit> OnSortConditionChanged {
		get {
			return this._onUpdateSortConditionChanged.AsObservable();
		}
	}

	/// <summary>
	/// IQueryable用ソート条件適用
	/// </summary>
	/// <param name="query">ソート対象のクエリ</param>
	/// <returns>ソート適用後クエリ</returns>
	public IQueryable<MediaItem> SetSortConditions(IQueryable<MediaItem> query) {
		var reverse = this.Direction.Value == ListSortDirection.Descending;
		if (this.CurrentSortCondition.Value is not { } cond) {
			return query;
		}
		if (cond.SortItemObjects.Count == 0) {
			throw new InvalidOperationException();
		}
		IOrderedQueryable<MediaItem>? sortedQuery = null;
		foreach (var si in cond.SortItemObjects.Select(SortItemFactory.Create)) {
			if (sortedQuery == null) {
				sortedQuery = si.ApplySort(query, reverse);
				continue;
			}

			sortedQuery = si.ApplyThenBySort(sortedQuery, reverse);
		}
		return sortedQuery!;
	}
}