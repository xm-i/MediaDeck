using MediaDeck.Common.Base;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Core.Models.Files.Filter;

namespace MediaDeck.ViewModels.Panes.FilterPanes;

/// <summary>
/// サイドペインで表示するフィルター条件の ViewModel クラス。
/// </summary>
public class FilteringConditionViewModel : ViewModelBase {
	private readonly IStringProvider _stringProvider;

	/// <summary>
	/// <see cref="FilteringConditionViewModel"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="model">フィルター条件モデル。</param>
	/// <param name="stringProvider">多言語リソースプロバイダー。</param>
	public FilteringConditionViewModel(FilteringCondition model, IStringProvider stringProvider) {
		this.Model = model;
		this._stringProvider = stringProvider;
		this.DisplayName = this.Model.DisplayName
			.ToBindableReactiveProperty(this.Model.DisplayName.CurrentValue ?? string.Empty)
			.AddTo(this.CompositeDisposable);

		this.FilterItems = this.Model.FilterItemObjects
			.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current)
			.AddTo(this.CompositeDisposable);

		this.ItemCount = new BindableReactiveProperty<int>(0).AddTo(this.CompositeDisposable);
		this.SummaryText = new BindableReactiveProperty<string>(string.Empty).AddTo(this.CompositeDisposable);
		this.DetailTooltip = new BindableReactiveProperty<string>(string.Empty).AddTo(this.CompositeDisposable);

		this.Model.FilterItemObjects.ObserveChanged()
			.Subscribe(_ => {
				this.UpdateSummary();
			})
			.AddTo(this.CompositeDisposable);

		this.UpdateSummary();
	}

	/// <summary>
	/// モデルを取得します。
	/// </summary>
	public FilteringCondition Model {
		get;
	}

	/// <summary>
	/// 表示名を取得します。
	/// </summary>
	public BindableReactiveProperty<string> DisplayName {
		get;
	}

	/// <summary>
	/// フィルター項目コレクションを取得します。
	/// </summary>
	public INotifyCollectionChangedSynchronizedViewList<IFilterItemObject> FilterItems {
		get;
	}

	/// <summary>
	/// 登録されているフィルタールール件数を取得します。
	/// </summary>
	public BindableReactiveProperty<int> ItemCount {
		get;
	}

	/// <summary>
	/// フィルター条件の概要テキストを取得します。
	/// </summary>
	public BindableReactiveProperty<string> SummaryText {
		get;
	}

	/// <summary>
	/// ホバー表示用の詳細ツールチップテキストを取得します。
	/// </summary>
	public BindableReactiveProperty<string> DetailTooltip {
		get;
	}

	private void UpdateSummary() {
		IReadOnlyList<IFilterItemObject> items = [.. this.Model.FilterItemObjects];
		this.ItemCount.Value = items.Count;
		this.SummaryText.Value = FilterItemDisplayHelper.FormatSummary(items, this._stringProvider);
		this.DetailTooltip.Value = FilterItemDisplayHelper.FormatDetailTooltip(items, this._stringProvider);
	}
}