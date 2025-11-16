using MediaDeck.Composition.Bases;
using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Models.Files.Filter;

namespace MediaDeck.ViewModels.Filters;

public class FilteringConditionEditorViewModel : ViewModelBase {
	/// <summary>
	/// モデル
	/// </summary>
	public FilteringConditionEditor Model {
		get;
	}

	/// <summary>
	/// 表示名
	/// </summary>
	public BindableReactiveProperty<string> DisplayName {
		get;
	}

	/// <summary>
	/// フィルター条件クリエイター
	/// </summary>
	public INotifyCollectionChangedSynchronizedViewList<IFilterItemObject> FilterItems {
		get;
	}

	/// <summary>
	/// フィルター削除コマンド
	/// </summary>
	public ReactiveCommand<IFilterItemObject> RemoveFilterCommand {
		get;
	} = new ReactiveCommand<IFilterItemObject>();


	public FilteringConditionEditorViewModel(FilteringConditionEditor model) {
		this.Model = model;
		this.DisplayName = this.Model.DisplayName.ToBindableReactiveProperty(null!);
		this.DisplayName.Subscribe(x => {
			this.Model.DisplayName.Value = x;
		});

		this.FilterItems = this.Model.FilterItemObjects.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);

		// 削除
		this.RemoveFilterCommand.Subscribe(this.Model.RemoveFilter).AddTo(this.CompositeDisposable);
	}

	public void AddFilter(IFilterItemObject filterItemObject) {
		this.Model.AddFilter(filterItemObject);
	}
}
