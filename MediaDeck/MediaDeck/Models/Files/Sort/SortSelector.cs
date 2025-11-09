using MediaDeck.FileTypes.Base.Models.Interfaces;
using MediaDeck.Models.Preferences;

using System.Collections.Generic;
using System.ComponentModel;

namespace MediaDeck.Models.Files.Sort;

[AddSingleton]
public class SortSelector {
	/// <summary>
	/// コンストラクタ
	/// </summary>
	public SortSelector(States states) {
		this._states = states;
		// 設定値初回値読み込み
		this.SortConditions.AddRange(this._states.SearchStates.SortConditions.Select(x => new SortCondition(x)));
		this.CurrentSortCondition.Value = this.SortConditions.FirstOrDefault(x => x.SortObject == states.SearchStates.CurrentSortCondition.Value);
		this.Direction.Value = this._states.SearchStates.SortDirection.Value;

		// 更新
		this.CurrentSortCondition.Select(_ => Unit.Default)
			.Merge(this.Direction.Select(_ => Unit.Default))
			.Subscribe(_ => {
				this._onUpdateSortConditionChanged.OnNext(Unit.Default);
			});

		IDisposable? before = null;
		this.CurrentSortCondition.Subscribe(x => {
			before?.Dispose();
			before = x?.OnUpdateSortConditions.Subscribe(_ => this._onUpdateSortConditionChanged.OnNext(Unit.Default));
		});


		this.CurrentSortCondition.Subscribe(x => {
			this._states.SearchStates.CurrentSortCondition.Value = x?.SortObject;
		});

		this._states.SearchStates.SortConditions.ObserveChanged().Subscribe(x => {
			this.SortConditions.Clear();
			this.SortConditions.AddRange(this._states.SearchStates.SortConditions.Select(x => new SortCondition(x)));
			this.CurrentSortCondition.Value = this.SortConditions.FirstOrDefault(x => x.DisplayName == this.CurrentSortCondition.Value?.DisplayName);
		});

		this.Direction.Subscribe(x => {
			this._states.SearchStates.SortDirection.Value = x;
		});
	}

	private readonly States _states;
	/// <summary>
	/// カレントソート条件
	/// </summary>
	public ReactiveProperty<SortCondition?> CurrentSortCondition {
		get;
	} = new();

	/// <summary>
	/// ソート条件リスト
	/// </summary>
	public ObservableList<SortCondition> SortConditions {
		get;
	} = [];

	/// <summary>
	/// ソート方向
	/// </summary>
	public ReactiveProperty<ListSortDirection> Direction {
		get;
	} = new();

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
		return this.CurrentSortCondition.Value?.ApplySort(array, this.Direction.Value == ListSortDirection.Descending) ?? array;
	}
}
