using MediaDeck.Common.Base;
using MediaDeck.Common.Extensions;
using MediaDeck.Core.Models.Files.Filter;
using MediaDeck.Core.Stores.State;

namespace MediaDeck.ViewModels.Panes.FilterPanes;

/// <summary>
/// フィルターセレクターViewModel
/// </summary>
[Inject(InjectServiceLifetime.Scoped)]
public class FilterSelectorViewModel : ViewModelBase {
	/// <summary>
	/// コンストラクタ
	/// </summary>
	public FilterSelectorViewModel(FilterSelector model, IStateStore stateStore) {
		this._stateStore = stateStore;
		this.FilteringConditions = model.FilteringConditions.CreateView(x => new FilteringConditionViewModel(x).AddTo(this.CompositeDisposable)).ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);

		this.SelectedConditions = model.CurrentFilteringConditions.ToTwoWayReactiveProperty(
			m => this.FilteringConditions.Where(fc => m.Contains(fc.Model)).ToArray(),
			vm => [.. vm.Select(fc => fc.Model)],
			[],
			this.CompositeDisposable).AddTo(this.CompositeDisposable);

		this.ChangeFilteringConditionSelectionCommand
			.Subscribe(selected => {
				this.SelectedConditions.Value = selected ?? [];
			}).AddTo(this.CompositeDisposable);
	}

	private readonly IStateStore _stateStore;

	/// <summary>
	/// 選択中のフィルター条件（Viewの複数選択と同期）
	/// </summary>
	public ReactiveProperty<FilteringConditionViewModel[]> SelectedConditions {
		get;
	} = new([]);

	/// <summary>
	/// フィルタリング条件
	/// </summary>
	public INotifyCollectionChangedSynchronizedViewList<FilteringConditionViewModel> FilteringConditions {
		get;
	}

	public ReactiveCommand<FilteringConditionViewModel[]?> ChangeFilteringConditionSelectionCommand {
		get;
	} = new();

	protected override void Dispose(bool disposing) {
		this._stateStore.Save();
		base.Dispose(disposing);
	}
}