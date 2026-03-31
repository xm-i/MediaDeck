using System.ComponentModel;

using MediaDeck.Common.Base;
using MediaDeck.Core.Models.Files;
using MediaDeck.Core.Models.Files.Sort;
using MediaDeck.Core.Stores.State;

namespace MediaDeck.ViewModels.Panes.ViewerPanes;

/// <summary>
/// ソートセレクターViewModel
/// </summary>
[Inject(InjectServiceLifetime.Singleton)]
public class SortSelectorViewModel : ViewModelBase {
	/// <summary>
	/// コンストラクタ
	/// </summary>
	public SortSelectorViewModel(SortSelector model, StateStore stateStore, MediaContentLibrary mediaContentLibrary) {
		this._stateStore = stateStore;
		this.SortConditions = model.SortConditions.CreateView(x => new SortConditionViewModel(x)).ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);
		this.CurrentCondition.Value = this.SortConditions.FirstOrDefault(c => c.Model == model.CurrentSortCondition.Value);
		this.CurrentCondition.Subscribe(async x => {
			model.CurrentSortCondition.Value = x?.Model;
			await mediaContentLibrary.SearchAsync();
		});
		this.Direction.Value = model.Direction.Value;
		this.Direction.Subscribe(async x => {
			model.Direction.Value = x;
			await mediaContentLibrary.SearchAsync();
		});
	}

	private readonly StateStore _stateStore;

	/// <summary>
	/// カレント条件
	/// </summary>
	public BindableReactiveProperty<SortConditionViewModel?> CurrentCondition {
		get;
	} = new();

	/// <summary>
	/// ソート条件
	/// </summary>
	public INotifyCollectionChangedSynchronizedViewList<SortConditionViewModel> SortConditions {
		get;
	}

	public BindableReactiveProperty<ListSortDirection> Direction {
		get;
	} = new();

	protected override void Dispose(bool disposing) {
		this._stateStore.Save();
		base.Dispose(disposing);
	}
}