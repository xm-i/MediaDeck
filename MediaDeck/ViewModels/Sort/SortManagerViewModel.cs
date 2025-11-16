using MediaDeck.Composition.Bases;
using MediaDeck.Models.Files.Sort;

namespace MediaDeck.ViewModels.Sort;

[AddTransient]
public class SortManagerViewModel : ViewModelBase {
	public SortManagerViewModel(SortManager sortManager) {
		this._sortManager = sortManager;

		this.AddSortConditionCommand.Subscribe(_ => {
			this.Add();
		});

		this.RemoveSortConditionCommand.Where(x => x != null).Subscribe(this.Remove);

		this.SaveCommand.Subscribe(_ => {
			this.Save();
		}).AddTo(this.CompositeDisposable);

		this.LoadCommand.Subscribe(_ => {
			this.Load();
		}).AddTo(this.CompositeDisposable);
		this.SortConditions = this._sortManager.SortConditions.CreateView(x => new SortConditionEditorViewModel(x)).ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);

	}

	private readonly SortManager _sortManager;

	/// <summary>
	/// カレント条件
	/// </summary>
	public BindableReactiveProperty<SortConditionEditorViewModel?> CurrentCondition {
		get;
	} = new();

	/// <summary>
	/// ソート条件
	/// </summary>
	public INotifyCollectionChangedSynchronizedViewList<SortConditionEditorViewModel> SortConditions {
		get;
	}

	/// <summary>
	/// ソート条件追加コマンド
	/// </summary>
	public ReactiveCommand AddSortConditionCommand {
		get;
	} = new();

	/// <summary>
	/// ソート条件削除コマンド
	/// </summary>
	public ReactiveCommand<SortConditionEditorViewModel> RemoveSortConditionCommand {
		get;
	} = new ReactiveCommand<SortConditionEditorViewModel>();

	/// <summary>
	/// 保存コマンド
	/// </summary>
	public ReactiveCommand SaveCommand {
		get;
	} = new();

	/// <summary>
	/// 読み込みコマンド
	/// </summary>
	public ReactiveCommand LoadCommand {
		get;
	} = new();

	/// <summary>
	/// 追加
	/// </summary>
	public void Add() {
		this._sortManager.AddCondition();
	}

	/// <summary>
	/// 削除
	/// </summary>
	public void Remove(SortConditionEditorViewModel filteringConditionViewModel) {
		this._sortManager.RemoveCondition(filteringConditionViewModel.Model);
	}

	/// <summary>
	/// 読み込み
	/// </summary>
	public void Load() {
		this.CurrentCondition.Value = this.SortConditions.FirstOrDefault();
	}

	/// <summary>
	/// 保存
	/// </summary>
	public void Save() {
		this._sortManager.Save();
	}
}
