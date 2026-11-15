using MediaDeck.Common.Base;
using MediaDeck.Common.Extensions;
using MediaDeck.Core.Models.Files.Filter;

namespace MediaDeck.ViewModels.Panes.FilterPanes;

/// <summary>
/// フィルターセレクターViewModel
/// </summary>
[Inject(InjectServiceLifetime.Scoped)]
public class FilterSelectorViewModel : ViewModelBase {
	private bool _isSyncStarted = false;
	/// <summary>
	/// コンストラクタ
	/// </summary>
	public FilterSelectorViewModel(FilterSelector model) {
		this.FilteringConditions = model.FilteringConditions
			.CreateViewWithDisposal(x => new FilteringConditionViewModel(x))
			.ToNotifyCollectionChangedWithDisposal(SynchronizationContextCollectionEventDispatcher.Current)
			.AddTo(this.CompositeDisposable);

		this.SelectedConditions = model.CurrentFilteringConditions.ToTwoWayReactiveProperty(
			m => this.FilteringConditions.Where(fc => m.Contains(fc.Model)).ToArray(),
			vm => [.. vm.Select(fc => fc.Model)],
			[],
			this.CompositeDisposable).AddTo(this.CompositeDisposable);

		this.ChangeFilteringConditionSelectionCommand
			.Where(_ => this._isSyncStarted)
			.Subscribe(selected => {
				this.SelectedConditions.Value = selected ?? [];
			}).AddTo(this.CompositeDisposable);

		this.UIReadyCommand.Subscribe(_ => {
			this._isSyncStarted = true;
		}).AddTo(this.CompositeDisposable);
	}

	/// <summary>
	/// UIの準備が完了したことを通知するためのコマンド。これが呼ばれるまでは、ChangeFilteringConditionSelectionCommandの実行を無視する。
	/// UI都合だけど、Tabを閉じて再度開いた場合UIの再利用がされる。
	/// そのケースではTabのLoadイベントが入る前にChangeFilteringConditionSelectionCommandが選択フィルターなし状態で呼ばれるため、無視する必要がある。
	/// </summary>
	public ReactiveCommand UIReadyCommand {
		get;
	} = new();

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
		base.Dispose(disposing);
	}
}