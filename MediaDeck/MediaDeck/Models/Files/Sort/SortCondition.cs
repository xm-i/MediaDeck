using System.Collections.Generic;

using MediaDeck.Composition.Bases;
using MediaDeck.FileTypes.Base.Models.Interfaces;
using MediaDeck.Utils.Enums;

namespace MediaDeck.Models.Files.Sort;
/// <summary>
/// ソート条件
/// </summary>
public class SortCondition : ModelBase {
	private readonly ISynchronizedView<SortItemCreator, ISortItem> _sortItems;

	public SortObject SortObject {
		get;
		private set;
	}

	/// <summary>
	/// 表示名
	/// </summary>
	public ReadOnlyReactiveProperty<string> DisplayName {
		get;
	}

	/// <summary>
	/// ソート条件
	/// </summary>
	public IReadOnlyObservableList<SortItemCreator> SortItemCreators {
		get;
	}

	/// <summary>
	/// ソート条件変更通知Subject
	/// </summary>
	private readonly Subject<Unit> _onUpdateSortConditions = new();

	/// <summary>
	/// ソート条件変更通知
	/// </summary>
	public Observable<Unit> OnUpdateSortConditions {
		get {
			return this._onUpdateSortConditions.AsObservable();
		}
	}

	/// <summary>
	///　設定用ソート項目リスト
	/// </summary>
	public ObservableList<SortItemCreator> CandidateSortItemCreators {
		get;
	} = [];

	/// <summary>
	/// コンストラクタ
	/// </summary>
	/// <param name="sortObject">保存/復元用ソートオブジェクト</param>
	public SortCondition(SortObject sortObject) {
		this.SortObject = sortObject;
		this.DisplayName = sortObject.DisplayName.ToReadOnlyReactiveProperty();
		this.SortItemCreators = sortObject.SortItemCreators;
		this._sortItems = this.SortItemCreators.CreateView(x => x.Create());

		this.SortItemCreators
			.ObserveChanged()
			.Subscribe(_ => {
				this._onUpdateSortConditions.OnNext(Unit.Default);
			}).AddTo(this.CompositeDisposable);

		this.CandidateSortItemCreators.AddRange(Enum.GetValues<SortItemKey>().Select(x => new SortItemCreator(x)));
	}

	/// <summary>
	/// ソート設定に従ってアイテムを整列して返却する。
	/// </summary>
	/// <param name="items">ソートを適用するアイテムリスト</param>
	/// <param name="reverse">ソート方向の反転を行うか否か true:反転する false:反転しない</param>
	/// <returns>結果</returns>
	public IOrderedEnumerable<IFileModel> ApplySort(IEnumerable<IFileModel> items, bool reverse) {
		if (this._sortItems.Count == 0) {
			throw new InvalidOperationException();
		}
		IOrderedEnumerable<IFileModel>? sortedItems = null;
		foreach (var si in this._sortItems) {
			if (sortedItems == null) {
				sortedItems = si.ApplySort(items, reverse);
				continue;
			}

			sortedItems = si.ApplyThenBySort(sortedItems, reverse);
		}
		return sortedItems!;
	}
}
