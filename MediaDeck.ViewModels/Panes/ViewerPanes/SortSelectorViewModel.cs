using System.ComponentModel;

using MediaDeck.Common.Base;
using MediaDeck.Common.Extensions;
using MediaDeck.Core.Models.Files.Sort;
using MediaDeck.Core.Stores.State;

namespace MediaDeck.ViewModels.Panes.ViewerPanes;

/// <summary>
/// ソートセレクターViewModel
/// </summary>
[Inject(InjectServiceLifetime.Scoped)]
public class SortSelectorViewModel : ViewModelBase {
	/// <summary>
	/// コンストラクタ
	/// </summary>
	public SortSelectorViewModel(SortSelector model, IStateStore stateStore) {
		this._stateStore = stateStore;
		this.SortConditions = model.SortConditions.CreateView(x => new SortConditionViewModel(x).AddTo(this.CompositeDisposable)).ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);
		this.CurrentCondition.Value = this.SortConditions.FirstOrDefault(c => c.Model == model.CurrentSortCondition.Value);
		this.CurrentCondition = model.CurrentSortCondition.ToTwoWayBindableReactiveProperty(
			x => this.SortConditions.FirstOrDefault(c => c.Model == x),
			x => x?.Model,
			null,
			this.CompositeDisposable);
		this.Direction.Value = model.Direction.Value;
		this.Direction = model.Direction.ToTwoWayBindableReactiveProperty(disposables: this.CompositeDisposable);
	}

	private readonly IStateStore _stateStore;

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