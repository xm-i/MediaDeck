using MediaDeck.Composition.Bases;
using MediaDeck.Models.Files;
using MediaDeck.Models.Files.Filter;
using MediaDeck.Models.Preferences;

namespace MediaDeck.ViewModels.Panes.FilterPanes;

/// <summary>
/// フィルターセレクターViewModel
/// </summary>
[AddSingleton]
public class FilterSelectorViewModel :ViewModelBase {
	/// <summary>
	/// コンストラクタ
	/// </summary>
	public FilterSelectorViewModel(FilterSelector model, States states,MediaContentLibrary mediaContentLibrary) {
		this._states = states;
		this.FilteringConditions = model.FilteringConditions.CreateView(x => new FilteringConditionViewModel(x)).ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);
		this.CurrentCondition = model.CurrentFilteringCondition.Select(x => this.FilteringConditions.FirstOrDefault(c => c.Model == x)).ToBindableReactiveProperty();
		this.ChangeFilteringConditionSelectionCommand.Subscribe(async x => {
			model.CurrentFilteringCondition.Value = x?.Model;
			await mediaContentLibrary.SearchAsync();
		});
	}

	private readonly States _states;
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
		this._states.Save();
		base.Dispose(disposing);
	}
}
