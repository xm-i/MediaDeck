using System.Collections.Generic;

using MediaDeck.Composition.Bases;
using MediaDeck.Composition.Interfaces.FileTypes.Models;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Database.Tables;

namespace MediaDeck.Models.Files.Filter;
/// <summary>
/// フィルターマネージャー
/// </summary>
[AddSingleton]
public class FilterSelector : ModelBase {
	/// <summary>
	/// コンストラクタ
	/// </summary>
	public FilterSelector(StateModel state) {
		this.FilteringConditions.AddRange(state.SearchState.FilteringConditions.Select(x => new FilteringCondition(x)));

		this.CurrentFilteringCondition.Value = this.FilteringConditions.FirstOrDefault(x => x.FilterObject.Id == state.SearchState.CurrentFilteringCondition.Value);

		IDisposable? beforeCurrent = null;
		this.CurrentFilteringCondition
			.Subscribe(x => {
				this._onUpdateFilteringChanged.OnNext(Unit.Default);
				beforeCurrent?.Dispose();
				beforeCurrent = x?.OnUpdateFilteringConditions
					.Subscribe(_ =>
						this._onUpdateFilteringChanged.OnNext(Unit.Default));
				state.SearchState.CurrentFilteringCondition.Value = x?.FilterObject.Id;
			})
			.AddTo(this.CompositeDisposable);

		state.SearchState.FilteringConditions.ObserveChanged().Subscribe(x => {
			this.FilteringConditions.Clear();
			this.FilteringConditions.AddRange(state.SearchState.FilteringConditions.Select(x => new FilteringCondition(x)));
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
