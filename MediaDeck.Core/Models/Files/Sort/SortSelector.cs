using System.ComponentModel;
using MediaDeck.Common.Base;
using MediaDeck.Common.Extensions;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.Notifications;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Composition.Stores.State.Model.Objects;
using MediaDeck.Composition.Tables;

namespace MediaDeck.Core.Models.Files.Sort;

[Inject(InjectServiceLifetime.Scoped)]
public class SortSelector : ModelBase {
	/// <summary>
	/// コンストラクタ
	/// </summary>
	public SortSelector(TabStateModel tabState, SearchDefinitionsConfigModel searchDefinitions, ISearchConditionNotificationDispatcher dispatcher) {
		this.RegenerateRandomSeed();

		// 設定値初回値読み込み
		this.SortConditions = searchDefinitions.SortConditions;
		this.CurrentSortCondition = tabState.SearchState.CurrentSortCondition.ToTwoWayReactiveProperty(x => this.SortConditions.FirstOrDefault(sc => sc.Id == x), x => x?.Id, null, this.CompositeDisposable).AddTo(this.CompositeDisposable);
		this.Direction = tabState.SearchState.SortDirection;

		this._onUpdateSortConditionChanged.AddTo(this.CompositeDisposable);

		// ソート条件変更を内部 Subject に通知しつつ、Dispatcher 経由で検索発火を依頼する
		this.CurrentSortCondition.Select(_ => Unit.Default)
			.Merge(this.Direction.Select(_ => Unit.Default))
			.Subscribe(_ => {
				this.RegenerateRandomSeed();
				this._onUpdateSortConditionChanged.OnNext(Unit.Default);
				dispatcher.SortChanged.OnNext(Unit.Default);
			})
			.AddTo(this.CompositeDisposable);

		var itemChangedSubscription = new SerialDisposable().AddTo(this.CompositeDisposable);
		this.CurrentSortCondition.Subscribe(x => {
			itemChangedSubscription.Disposable = x?.SortItemObjects.ObserveChanged().Subscribe(_ => {
				this.RegenerateRandomSeed();
				this._onUpdateSortConditionChanged.OnNext(Unit.Default);
				dispatcher.SortChanged.OnNext(Unit.Default);
			});
		}).AddTo(this.CompositeDisposable);

		// 手動リロード時にもシード値を更新
		dispatcher.ReloadRequested?.Subscribe(_ => this.RegenerateRandomSeed())
				.AddTo(this.CompositeDisposable);
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
	/// ランダムソート用シード値
	/// </summary>
	public int RandomSeed {
		get;
		private set;
	}

	/// <summary>
	/// 現在選択されているソート条件にランダムソートが含まれているかどうかを取得します。
	/// </summary>
	public bool IsRandomSort {
		get {
			var cond = this.CurrentSortCondition.Value;
			if (cond == null || cond.SortItemObjects.Count == 0) {
				cond = this.SortConditions.FirstOrDefault(x => x.SortItemObjects.Count > 0);
			}
			return cond?.SortItemObjects.Any(x => x.SortItemKey == SortItemKey.Random) ?? false;
		}
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
	/// ランダムソート用のシード値を新しく再生成します。
	/// </summary>
	public void RegenerateRandomSeed() {
		this.RandomSeed = Random.Shared.Next(1, 1_000_000_000);
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
			cond = this.SortConditions.FirstOrDefault(x => x.SortItemObjects.Count > 0);
			if (cond == null || cond.SortItemObjects.Count == 0) {
				return query;
			}
		}
		IOrderedQueryable<MediaItem>? sortedQuery = null;
		foreach (var si in cond.SortItemObjects.Select(sio => SortItemFactory.Create(sio, this.RandomSeed))) {
			if (sortedQuery == null) {
				sortedQuery = si.ApplySort(query, reverse);
				continue;
			}

			sortedQuery = si.ApplyThenBySort(sortedQuery, reverse);
		}
		return sortedQuery ?? query;
	}
}