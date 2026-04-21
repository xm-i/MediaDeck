using MediaDeck.Common.Base;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Core.Models.NotificationDispatcher;
using MediaDeck.Database.Tables;

namespace MediaDeck.Core.Models.Files.Filter;

/// <summary>
/// フィルターマネージャー
/// </summary>
[Inject(InjectServiceLifetime.Scoped)]
public class FilterSelector : ModelBase {
	/// <summary>
	/// コンストラクタ
	/// </summary>
	public FilterSelector(TabStateModel tabState, SearchDefinitionsStateModel searchDefinitions, SearchConditionNotificationDispatcher dispatcher) {
		this.FilteringConditions.AddRange(searchDefinitions.FilteringConditions.Select(x => new FilteringCondition(x)));

		this.CurrentFilteringCondition.Value = this.FilteringConditions.FirstOrDefault(x => x.FilterObject.Id == tabState.SearchState.CurrentFilteringCondition.Value);

		IDisposable? beforeCurrent = null;
		this.CurrentFilteringCondition
			.Subscribe(x => {
				// フィルター条件変更を内部 Subject に通知し、Dispatcher 経由で検索発火を依頼する
				this._onUpdateFilteringChanged.OnNext(Unit.Default);
				dispatcher.FilterChanged.OnNext(Unit.Default);
				beforeCurrent?.Dispose();
				beforeCurrent = x?.OnUpdateFilteringConditions
					.Subscribe(_ => {
						this._onUpdateFilteringChanged.OnNext(Unit.Default);
						dispatcher.FilterChanged.OnNext(Unit.Default);
					});
				tabState.SearchState.CurrentFilteringCondition.Value = x?.FilterObject.Id;
			})
			.AddTo(this.CompositeDisposable);

		searchDefinitions.FilteringConditions.ObserveChanged()
			.Subscribe(x => {
				this.FilteringConditions.Clear();
				this.FilteringConditions.AddRange(searchDefinitions.FilteringConditions.Select(x => new FilteringCondition(x)));
				this.CurrentFilteringCondition.Value = this.FilteringConditions.FirstOrDefault(x => x.DisplayName == this.CurrentFilteringCondition.Value?.DisplayName);
			})
			.AddTo(this.CompositeDisposable);
	}

	/// <summary>
	/// フィルター条件変更通知Subject
	/// </summary>
	private readonly Subject<Unit> _onUpdateFilteringChanged = new();

	/// <summary>
	/// フィルター条件変更通知
	/// </summary>
	public Observable<Unit> OnFilteringConditionChanged {
		get {
			return this._onUpdateFilteringChanged.AsObservable();
		}
	}

	/// <summary>
	/// カレント条件
	/// </summary>
	public ReactiveProperty<FilteringCondition?> CurrentFilteringCondition {
		get;
	} = new();

	/// <summary>
	/// フィルター条件リスト
	/// </summary>
	public ObservableList<FilteringCondition> FilteringConditions {
		get;
	} = [];

	/// <summary>
	/// フィルターマネージャーで選択したフィルターを引数に渡されたクエリに適用して返却する。
	/// </summary>
	/// <param name="query">絞り込みクエリを適用するクエリ</param>
	/// <returns>フィルター適用後クエリ</returns>
	public IQueryable<MediaFile> SetFilterConditions(IQueryable<MediaFile> query) {
		return this.CurrentFilteringCondition.Value?.SetFilterConditions(query) ?? query;
	}

}