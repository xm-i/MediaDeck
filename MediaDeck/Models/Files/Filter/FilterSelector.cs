using System.Collections.Generic;
using System.Reactive.Linq;

using MediaDeck.Composition.Bases;
using MediaDeck.Database.Tables;
using MediaDeck.FileTypes.Base.Models.Interfaces;
using MediaDeck.Models.Preferences;

namespace MediaDeck.Models.Files.Filter;
/// <summary>
/// フィルターマネージャー
/// </summary>
[AddSingleton]
public class FilterSelector : ModelBase {
	/// <summary>
	/// コンストラクタ
	/// </summary>
	public FilterSelector(States states) {
		this.FilteringConditions.AddRange(states.SearchStates.FilteringConditions.Select(x => new FilteringCondition(x)));

		this.CurrentFilteringCondition.Value = this.FilteringConditions.FirstOrDefault(x => x.FilterObject == states.SearchStates.CurrentFilteringCondition.Value);

		IDisposable? beforeCurrent = null;
		this.CurrentFilteringCondition
			.Subscribe(x => {
				this._onUpdateFilteringChanged.OnNext(Unit.Default);
				beforeCurrent?.Dispose();
				beforeCurrent = x?.OnUpdateFilteringConditions
					.Subscribe(_ =>
						this._onUpdateFilteringChanged.OnNext(Unit.Default));
				states.SearchStates.CurrentFilteringCondition.Value = x?.FilterObject;
			})
			.AddTo(this.CompositeDisposable);

		states.SearchStates.FilteringConditions.ObserveChanged().Subscribe(x => {
			this.FilteringConditions.Clear();
			this.FilteringConditions.AddRange(states.SearchStates.FilteringConditions.Select(x => new FilteringCondition(x)));
			this.CurrentFilteringCondition.Value = this.FilteringConditions.FirstOrDefault(x => x.DisplayName == this.CurrentFilteringCondition.Value?.DisplayName);
		});
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
	public IEnumerable<MediaFile> SetFilterConditions(IQueryable<MediaFile> query) {
		return this.CurrentFilteringCondition.Value?.SetFilterConditions(query) ?? query;
	}

	/// <summary>
	/// フィルターマネージャーで選択したフィルターを引数に渡されたシーケンスに適用して返却する。
	/// </summary>
	/// <param name="query">絞り込みを適用するシーケンス</param>
	/// <returns>フィルター適用後シーケンス</returns>
	public IEnumerable<IFileModel> SetFilterConditions(IEnumerable<IFileModel> files) {
		return this.CurrentFilteringCondition.Value?.SetFilterConditions(files) ?? files;
	}
}
