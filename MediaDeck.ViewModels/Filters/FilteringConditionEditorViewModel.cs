using MediaDeck.Common.Base;
using MediaDeck.Common.Extensions;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Core.Models.Files.Filter;

namespace MediaDeck.ViewModels.Filters;

/// <summary>
/// 単一のフィルター条件セットを編集するための ViewModel クラス。
/// </summary>
public class FilteringConditionEditorViewModel : ViewModelBase {
	private readonly IStringProvider _stringProvider;

	/// <summary>
	/// <see cref="FilteringConditionEditorViewModel"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="model">フィルター条件編集モデル。</param>
	/// <param name="stringProvider">多言語リソースプロバイダー。</param>
	public FilteringConditionEditorViewModel(FilteringConditionEditor model, IStringProvider stringProvider) {
		this.Model = model;
		this._stringProvider = stringProvider;

		this.DisplayName = this.Model.DisplayName
			.ToTwoWayBindableReactiveProperty(this.Model.DisplayName.Value ?? string.Empty, this.CompositeDisposable);

		this.FilterItems = this.Model.FilterItemObjects
			.CreateViewWithDisposal(x => new FilterItemDisplayViewModel(x, this._stringProvider, this.RemoveFilter))
			.ToNotifyCollectionChangedWithDisposal(SynchronizationContextCollectionEventDispatcher.Current)
			.AddTo(this.CompositeDisposable);

		this.FilterItemCount = new BindableReactiveProperty<int>(0).AddTo(this.CompositeDisposable);
		this.HasFilterItems = new BindableReactiveProperty<bool>(false).AddTo(this.CompositeDisposable);
		this.IsEmpty = new BindableReactiveProperty<bool>(true).AddTo(this.CompositeDisposable);
		this.SummaryText = new BindableReactiveProperty<string>(string.Empty).AddTo(this.CompositeDisposable);

		this.RemoveFilterCommand = new ReactiveCommand<IFilterItemObject>().AddTo(this.CompositeDisposable);
		this.RemoveFilterCommand.Subscribe(this.RemoveFilter).AddTo(this.CompositeDisposable);

		this.Model.FilterItemObjects.ObserveChanged()
			.Subscribe(_ => {
				this.UpdateFilterItemStates();
			})
			.AddTo(this.CompositeDisposable);

		this.UpdateFilterItemStates();
	}

	/// <summary>
	/// モデルを取得します。
	/// </summary>
	public FilteringConditionEditor Model {
		get;
	}

	/// <summary>
	/// 表示名を取得します。
	/// </summary>
	public BindableReactiveProperty<string> DisplayName {
		get;
	}

	/// <summary>
	/// UI表示用の適用済みフィルター項目一覧を取得します。
	/// </summary>
	public INotifyCollectionChangedSynchronizedViewList<FilterItemDisplayViewModel> FilterItems {
		get;
	}

	/// <summary>
	/// 登録されているフィルター項目の件数を取得します。
	/// </summary>
	public BindableReactiveProperty<int> FilterItemCount {
		get;
	}

	/// <summary>
	/// フィルター項目が1件以上登録されているかどうかを取得します。
	/// </summary>
	public BindableReactiveProperty<bool> HasFilterItems {
		get;
	}

	/// <summary>
	/// フィルター項目が0件であるかどうかを取得します。
	/// </summary>
	public BindableReactiveProperty<bool> IsEmpty {
		get;
	}

	/// <summary>
	/// 条件の概要テキストを取得します。
	/// </summary>
	public BindableReactiveProperty<string> SummaryText {
		get;
	}

	/// <summary>
	/// 指定したフィルター項目を削除するコマンドを取得します。
	/// </summary>
	public ReactiveCommand<IFilterItemObject> RemoveFilterCommand {
		get;
	}

	/// <summary>
	/// 新しいフィルター項目を追加します。
	/// </summary>
	/// <param name="filterItemObject">追加するフィルター項目オブジェクト。</param>
	public void AddFilter(IFilterItemObject filterItemObject) {
		this.Model.AddFilter(filterItemObject);
	}

	/// <summary>
	/// 指定したフィルター項目を削除します。
	/// </summary>
	/// <param name="filterItemObject">削除するフィルター項目オブジェクト。</param>
	public void RemoveFilter(IFilterItemObject filterItemObject) {
		this.Model.RemoveFilter(filterItemObject);
	}

	private void UpdateFilterItemStates() {
		var count = this.Model.FilterItemObjects.Count;
		this.FilterItemCount.Value = count;
		this.HasFilterItems.Value = count > 0;
		this.IsEmpty.Value = count == 0;
		this.SummaryText.Value = FilterItemDisplayHelper.FormatSummary(this.Model.FilterItemObjects, this._stringProvider);
	}
}