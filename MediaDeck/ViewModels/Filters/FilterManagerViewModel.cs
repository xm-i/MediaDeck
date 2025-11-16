
using System.Collections.Generic;

using MediaDeck.Composition.Bases;
using MediaDeck.Models.Files.Filter;
using MediaDeck.ViewModels.Filters.FilterItemCreators;

namespace MediaDeck.ViewModels.Filters;

[AddTransient]
public class FilterManagerViewModel : ViewModelBase {
	private readonly FilterManager _filterManager;

	/// <summary>
	/// カレント条件
	/// </summary>
	public BindableReactiveProperty<FilteringConditionEditorViewModel?> CurrentCondition {
		get;
	} = new();

	/// <summary>
	/// フィルタリング条件
	/// </summary>
	public INotifyCollectionChangedSynchronizedViewList<FilteringConditionEditorViewModel> FilteringConditions {
		get;
	}

	/// <summary>
	/// フィルタリング条件追加コマンド
	/// </summary>
	public ReactiveCommand AddFilteringConditionCommand {
		get;
	} = new();

	/// <summary>
	/// フィルタリング条件削除コマンド
	/// </summary>
	public ReactiveCommand<FilteringConditionEditorViewModel> RemoveFilteringConditionCommand {
		get;
	} = new ReactiveCommand<FilteringConditionEditorViewModel>();

	/// <summary>
	/// 保存コマンド
	/// </summary>
	public ReactiveCommand SaveCommand {
		get;
	} = new();

	/// <summary>
	/// フィルター条件作成VMリスト
	/// </summary>
	public IEnumerable<IFilterCreatorViewModel> FilterCreatorViewModels {
		get;
	}

	/// <summary>
	/// 選択中フィルター条件作成VM
	/// </summary>
	public BindableReactiveProperty<IFilterCreatorViewModel> SelectedFilterCreatorViewModel {
		get;
	} = new();

	/// <summary>
	/// コンストラクタ
	/// </summary>
	public FilterManagerViewModel(FilterManager filterManager) {
		this._filterManager = filterManager;

		this.AddFilteringConditionCommand.Subscribe(_ => {
			this.Add();
		});

		this.RemoveFilteringConditionCommand.Where(x => x != null).Subscribe(this.Remove);

		this.SaveCommand.Subscribe(_ => {
			this.Save();
		}).AddTo(this.CompositeDisposable);

		this.FilteringConditions = filterManager.FilteringConditions.CreateView(x => new FilteringConditionEditorViewModel(x)).ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);

		this.FilterCreatorViewModels = [
				new ExistsFilterCreatorViewModel(this.CurrentCondition),
				new FilePathFilterCreatorViewModel(this.CurrentCondition),
				new LocationFilterCreatorViewModel(this.CurrentCondition),
				new MediaTypeFilterCreatorViewModel(this.CurrentCondition),
				new RateFilterCreatorViewModel(this.CurrentCondition),
				new ResolutionFilterCreatorViewModel(this.CurrentCondition),
				new TagFilterCreatorViewModel(this.CurrentCondition)
			];
		this.SelectedFilterCreatorViewModel.Value = this.FilterCreatorViewModels.First();
	}

	/// <summary>
	/// 追加
	/// </summary>
	public void Add() {
		this._filterManager.AddCondition();
	}

	/// <summary>
	/// 削除
	/// </summary>
	public void Remove(FilteringConditionEditorViewModel filteringConditionViewModel) {
		this._filterManager.RemoveCondition(filteringConditionViewModel.Model);
	}

	/// <summary>
	/// 保存
	/// </summary>
	public void Save() {
		this._filterManager.Save();
	}
}
