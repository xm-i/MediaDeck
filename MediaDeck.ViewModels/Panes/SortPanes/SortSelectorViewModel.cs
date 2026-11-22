using System.ComponentModel;

using MediaDeck.Common.Base;
using MediaDeck.Common.Extensions;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Core.Models.Files.Sort;
using MediaDeck.Core.Stores.State;

namespace MediaDeck.ViewModels.Panes.SortPanes;

/// <summary>
/// ソートセレクターViewModel
/// </summary>
[Inject(InjectServiceLifetime.Scoped)]
public class SortSelectorViewModel : ViewModelBase {
	private readonly IStateStore _stateStore;

	/// <summary>
	/// <see cref="SortSelectorViewModel"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="model">ソートセレクターモデル。</param>
	/// <param name="stateStore">状態ストア。</param>
	/// <param name="stringProvider">多言語リソースプロバイダー。</param>
	public SortSelectorViewModel(SortSelector model, IStateStore stateStore, IStringProvider stringProvider) {
		this._stateStore = stateStore;
		this.SortConditions = model.SortConditions
			.CreateViewWithDisposal(x => new SortConditionViewModel(x, stringProvider))
			.ToNotifyCollectionChangedWithDisposal(SynchronizationContextCollectionEventDispatcher.Current)
			.AddTo(this.CompositeDisposable);
		this.CurrentCondition = model.CurrentSortCondition.ToTwoWayBindableReactiveProperty(
			x => this.SortConditions.FirstOrDefault(c => c.Model == x),
			x => x?.Model,
			this.SortConditions.FirstOrDefault(c => c.Model == model.CurrentSortCondition.Value),
			this.CompositeDisposable);
		this.Direction = model.Direction.ToTwoWayBindableReactiveProperty(model.Direction.Value, this.CompositeDisposable);

		this.DirectionDisplayName = this.Direction
			.Select(dir => SortDisplayHelper.GetDirectionDisplayName(dir, stringProvider))
			.ToBindableReactiveProperty(SortDisplayHelper.GetDirectionDisplayName(this.Direction.Value, stringProvider))
			.AddTo(this.CompositeDisposable);

		this.CurrentConditionSummary = new BindableReactiveProperty<string>(string.Empty)
			.AddTo(this.CompositeDisposable);

		var summarySubscription = new SerialDisposable().AddTo(this.CompositeDisposable);
		this.CurrentCondition.Subscribe(cond => {
			if (cond is null) {
				summarySubscription.Disposable = null;
				this.CurrentConditionSummary.Value = string.Empty;
				return;
			}
			summarySubscription.Disposable = cond.SummaryText.Subscribe(summary => {
				this.CurrentConditionSummary.Value = summary;
			});
		}).AddTo(this.CompositeDisposable);
	}

	/// <summary>
	/// カレント条件を取得します。
	/// </summary>
	public BindableReactiveProperty<SortConditionViewModel?> CurrentCondition {
		get;
	}

	/// <summary>
	/// ソート条件一覧を取得します。
	/// </summary>
	public INotifyCollectionChangedSynchronizedViewList<SortConditionViewModel> SortConditions {
		get;
	}

	/// <summary>
	/// ソート方向を取得します。
	/// </summary>
	public BindableReactiveProperty<ListSortDirection> Direction {
		get;
	}

	/// <summary>
	/// 現在のソート方向の表示名を取得します。
	/// </summary>
	public BindableReactiveProperty<string> DirectionDisplayName {
		get;
	}

	/// <summary>
	/// 選択中ソート条件の概要テキストを取得します。
	/// </summary>
	public BindableReactiveProperty<string> CurrentConditionSummary {
		get;
	}

	/// <inheritdoc />
	protected override void Dispose(bool disposing) {
		base.Dispose(disposing);
	}
}