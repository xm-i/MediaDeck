using MediaDeck.Common.Base;
using MediaDeck.Core.Models.Files;
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
	public FilterSelectorViewModel(FilterSelector model, IStateStore stateStore, MediaContentLibrary mediaContentLibrary) {
		this._stateStore = stateStore;
		this.FilteringConditions = model.FilteringConditions.CreateView(x => new FilteringConditionViewModel(x)).ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);
		this.CurrentCondition = model.CurrentFilteringCondition.Select(x => this.FilteringConditions.FirstOrDefault(c => c.Model == x)).ToBindableReactiveProperty();
		this.ChangeFilteringConditionSelectionCommand.Subscribe(async x => {
			model.CurrentFilteringCondition.Value = x?.Model;
			await mediaContentLibrary.SearchAsync();
		});
	}

	private readonly IStateStore _stateStore;

	/// <summary>
	/// カレント条件
	/// </summary>
	public BindableReactiveProperty<FilteringConditionViewModel?> CurrentCondition {
		get;
	}

	/// <summary>
	/// フィルタリング条件
	/// </summary>
	public INotifyCollectionChangedSynchronizedViewList<FilteringConditionViewModel> FilteringConditions {
		get;
	}

	public ReactiveCommand<FilteringConditionViewModel> ChangeFilteringConditionSelectionCommand {
		get;
	} = new();

	protected override void Dispose(bool disposing) {
		this._stateStore.Save();
		base.Dispose(disposing);
	}
}