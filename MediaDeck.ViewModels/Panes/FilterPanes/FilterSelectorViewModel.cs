using MediaDeck.Common.Base;
using MediaDeck.Common.Extensions;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Core.Models.Files.Filter;

namespace MediaDeck.ViewModels.Panes.FilterPanes;

/// <summary>
/// フィルターセレクターViewModel
/// </summary>
[Inject(InjectServiceLifetime.Scoped)]
public class FilterSelectorViewModel : ViewModelBase {
	private bool _isSyncStarted;

	/// <summary>
	/// <see cref="FilterSelectorViewModel"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="model">フィルターセレクターモデル。</param>
	/// <param name="stringProvider">多言語リソースプロバイダー。</param>
	public FilterSelectorViewModel(FilterSelector model, IStringProvider stringProvider) {
		this.UIReadyCommand = new ReactiveCommand().AddTo(this.CompositeDisposable);
		this.ChangeFilteringConditionSelectionCommand = new ReactiveCommand<FilteringConditionViewModel[]?>().AddTo(this.CompositeDisposable);
		this.ClearSelectionCommand = new ReactiveCommand().AddTo(this.CompositeDisposable);

		this.FilteringConditions = model.FilteringConditions
			.CreateViewWithDisposal(x => new FilteringConditionViewModel(x, stringProvider))
			.ToNotifyCollectionChangedWithDisposal(SynchronizationContextCollectionEventDispatcher.Current)
			.AddTo(this.CompositeDisposable);

		this.SelectedConditions = model.CurrentFilteringConditions.ToTwoWayReactiveProperty(
			m => this.FilteringConditions.Where(fc => m.Contains(fc.Model)).ToArray(),
			vm => [.. vm.Select(fc => fc.Model)],
			[],
			this.CompositeDisposable);

		this.IsEmpty = model.FilteringConditions
			.ObserveCountChanged()
			.Select(c => c == 0)
			.ToBindableReactiveProperty(model.FilteringConditions.Count == 0)
			.AddTo(this.CompositeDisposable);

		this.SelectedCount = this.SelectedConditions
			.Select(selected => selected?.Length ?? 0)
			.ToBindableReactiveProperty(this.SelectedConditions.Value?.Length ?? 0)
			.AddTo(this.CompositeDisposable);

		this.HasSelectedConditions = this.SelectedCount
			.Select(count => count > 0)
			.ToBindableReactiveProperty(this.SelectedCount.Value > 0)
			.AddTo(this.CompositeDisposable);

		this.ChangeFilteringConditionSelectionCommand
			.Where(_ => this._isSyncStarted)
			.Subscribe(selected => {
				this.SelectedConditions.Value = selected ?? [];
			}).AddTo(this.CompositeDisposable);

		this.ClearSelectionCommand.Subscribe(_ => {
			this.SelectedConditions.Value = [];
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
	}

	/// <summary>
	/// 選択中のフィルター条件（Viewの複数選択と同期）を取得します。
	/// </summary>
	public ReactiveProperty<FilteringConditionViewModel[]> SelectedConditions {
		get;
	}

	/// <summary>
	/// フィルタリング条件一覧を取得します。
	/// </summary>
	public INotifyCollectionChangedSynchronizedViewList<FilteringConditionViewModel> FilteringConditions {
		get;
	}

	/// <summary>
	/// フィルター条件が0件であるかどうかを取得します。
	/// </summary>
	public BindableReactiveProperty<bool> IsEmpty {
		get;
	}

	/// <summary>
	/// 適用中のフィルター条件数を取得します。
	/// </summary>
	public BindableReactiveProperty<int> SelectedCount {
		get;
	}

	/// <summary>
	/// 1件以上のフィルター条件が適用中であるかどうかを取得します。
	/// </summary>
	public BindableReactiveProperty<bool> HasSelectedConditions {
		get;
	}

	/// <summary>
	/// 選択状態のフィルター条件を変更するコマンドを取得します。
	/// </summary>
	public ReactiveCommand<FilteringConditionViewModel[]?> ChangeFilteringConditionSelectionCommand {
		get;
	}

	/// <summary>
	/// 選択中のフィルター条件をすべて解除するコマンドを取得します。
	/// </summary>
	public ReactiveCommand ClearSelectionCommand {
		get;
	}

	/// <inheritdoc />
	protected override void Dispose(bool disposing) {
		base.Dispose(disposing);
	}
}