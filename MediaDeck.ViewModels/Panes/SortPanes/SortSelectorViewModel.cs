using System.ComponentModel;

using MediaDeck.Common.Base;
using MediaDeck.Common.Extensions;
using MediaDeck.Core.Models.Files.Sort;
using MediaDeck.Core.Stores.State;

namespace MediaDeck.ViewModels.Panes.SortPanes;

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
		this.SortConditions = model.SortConditions
			.CreateViewWithDisposal(x => new SortConditionViewModel(x))
			.ToNotifyCollectionChangedWithDisposal(SynchronizationContextCollectionEventDispatcher.Current)
			.AddTo(this.CompositeDisposable);
		this.CurrentCondition = model.CurrentSortCondition.ToTwoWayBindableReactiveProperty(
			x => this.SortConditions.FirstOrDefault(c => c.Model == x),
			x => x?.Model,
			this.SortConditions.FirstOrDefault(c => c.Model == model.CurrentSortCondition.Value),
			this.CompositeDisposable);
		this.Direction = model.Direction.ToTwoWayBindableReactiveProperty(model.Direction.Value, this.CompositeDisposable);
	}

	private readonly IStateStore _stateStore;

	/// <summary>
	/// カレント条件
	/// </summary>
	public BindableReactiveProperty<SortConditionViewModel?> CurrentCondition {
		get;
	}

	/// <summary>
	/// ソート条件
	/// </summary>
	public INotifyCollectionChangedSynchronizedViewList<SortConditionViewModel> SortConditions {
		get;
	}

	/// <summary>
	/// ソート方向
	/// </summary>
	public BindableReactiveProperty<ListSortDirection> Direction {
		get;
	}

	protected override void Dispose(bool disposing) {
		base.Dispose(disposing);
	}
}