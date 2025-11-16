using System.Collections.Generic;
using System.ComponentModel;

using MediaDeck.Composition.Bases;
using MediaDeck.Composition.Interfaces.FileTypes.Models;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Composition.Stores.State.Model.Objects;
using MediaDeck.Utils.Tools;

namespace MediaDeck.Models.Files.Sort;

[AddSingleton]
public class SortSelector: ModelBase {
	/// <summary>
	/// コンストラクタ
	/// </summary>
	public SortSelector(StateModel state) {
		// 設定値初回値読み込み
		this.SortConditions = state.SearchState.SortConditions;
		this.CurrentSortCondition = state.SearchState.CurrentSortCondition.ToTwoWayReactiveProperty(x => this.SortConditions.FirstOrDefault(sc => sc.Id == x), x => x?.Id, null);
		this.Direction = state.SearchState.SortDirection;

		// 更新
		this.CurrentSortCondition.Select(_ => Unit.Default)
			.Merge(this.Direction.Select(_ => Unit.Default))
			.Subscribe(_ => {
				this._onUpdateSortConditionChanged.OnNext(Unit.Default);
			}).AddTo(this.CompositeDisposable);

		IDisposable? before = null;
		this.CurrentSortCondition.Subscribe(x => {
			before?.Dispose();
			before = x?.SortItemObjects.ObserveChanged().Subscribe(_ => this._onUpdateSortConditionChanged.OnNext(Unit.Default));
		});
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
	/// ソート条件適用
	/// </summary>
	/// <param name="array">ソート対象の配列</param>
	/// <returns>ソート済み配列</returns>
	public IEnumerable<IFileModel> SetSortConditions(IEnumerable<IFileModel> array) {
		var reverse = this.Direction.Value == ListSortDirection.Descending;
		if (this.CurrentSortCondition.Value is not { } cond) {
			return array;
		}
		if (cond.SortItemObjects.Count == 0) {
			throw new InvalidOperationException();
		}
		IOrderedEnumerable<IFileModel>? sortedItems = null;
		foreach (var si in cond.SortItemObjects.Select(SortItemFactory.Create)) {
			if (sortedItems == null) {
				sortedItems = si.ApplySort(array, reverse);
				continue;
			}

			sortedItems = si.ApplyThenBySort(sortedItems, reverse);
		}
		return sortedItems!;
	}
}
