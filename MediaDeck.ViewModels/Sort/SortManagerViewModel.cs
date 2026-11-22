using MediaDeck.Common.Base;
using MediaDeck.Common.Extensions;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Core.Models.Files.Sort;

namespace MediaDeck.ViewModels.Sort;

/// <summary>
/// ソート管理画面の ViewModel クラス。
/// </summary>
[Inject(InjectServiceLifetime.Transient)]
public class SortManagerViewModel : ViewModelBase {
	private readonly SortManager _sortManager;

	/// <summary>
	/// <see cref="SortManagerViewModel"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="sortManager">ソートマネージャー。</param>
	/// <param name="stringProvider">多言語リソースプロバイダー。</param>
	public SortManagerViewModel(SortManager sortManager, IStringProvider stringProvider) {
		this._sortManager = sortManager;

		this.CurrentCondition = new BindableReactiveProperty<SortConditionEditorViewModel?>().AddTo(this.CompositeDisposable);
		this.AddSortConditionCommand = new ReactiveCommand().AddTo(this.CompositeDisposable);
		this.RemoveSortConditionCommand = new ReactiveCommand<SortConditionEditorViewModel>().AddTo(this.CompositeDisposable);
		this.SaveCommand = new ReactiveCommand().AddTo(this.CompositeDisposable);
		this.ApplyCommand = new ReactiveCommand().AddTo(this.CompositeDisposable);
		this.LoadCommand = new ReactiveCommand().AddTo(this.CompositeDisposable);

		var candidateSortKeys = SortConditionEditorViewModel.CreateCandidateSortKeys(stringProvider);
		var directionOptions = SortConditionEditorViewModel.CreateDirectionOptions(stringProvider);

		this.SortConditions = this._sortManager.SortConditions
			.CreateViewWithDisposal(x => new SortConditionEditorViewModel(x, stringProvider, candidateSortKeys, directionOptions))
			.ToNotifyCollectionChangedWithDisposal(SynchronizationContextCollectionEventDispatcher.Current)
			.AddTo(this.CompositeDisposable);

		this.ConditionCount = this._sortManager.SortConditions
			.ObserveCountChanged()
			.ToBindableReactiveProperty(this._sortManager.SortConditions.Count)
			.AddTo(this.CompositeDisposable);

		this.IsEmpty = this.ConditionCount
			.Select(c => c == 0)
			.ToBindableReactiveProperty(this._sortManager.SortConditions.Count == 0)
			.AddTo(this.CompositeDisposable);

		this.HasCurrentCondition = this.CurrentCondition
			.Select(x => x is not null)
			.ToBindableReactiveProperty(false)
			.AddTo(this.CompositeDisposable);

		this.ShowEmptyMessage = this.CurrentCondition
			.CombineLatest(this.IsEmpty, (current, isEmpty) => current is null && isEmpty)
			.ToBindableReactiveProperty(false)
			.AddTo(this.CompositeDisposable);

		this.ShowSelectConditionMessage = this.CurrentCondition
			.CombineLatest(this.IsEmpty, (current, isEmpty) => current is null && !isEmpty)
			.ToBindableReactiveProperty(false)
			.AddTo(this.CompositeDisposable);

		this.AddSortConditionCommand.Subscribe(_ => {
			this.Add();
		}).AddTo(this.CompositeDisposable);

		this.RemoveSortConditionCommand
			.Where(x => x != null)
			.Subscribe(this.Remove)
			.AddTo(this.CompositeDisposable);

		this.SaveCommand.Subscribe(_ => {
			this.Save();
			this.Close();
		}).AddTo(this.CompositeDisposable);

		this.ApplyCommand.Subscribe(_ => {
			this.Save();
		}).AddTo(this.CompositeDisposable);

		this.LoadCommand.Subscribe(_ => {
			this.Load();
		}).AddTo(this.CompositeDisposable);

		this.CurrentCondition.Value = this.SortConditions.FirstOrDefault();
	}

	/// <summary>
	/// カレント条件を取得します。
	/// </summary>
	public BindableReactiveProperty<SortConditionEditorViewModel?> CurrentCondition {
		get;
	}

	/// <summary>
	/// ソート条件一覧を取得します。
	/// </summary>
	public INotifyCollectionChangedSynchronizedViewList<SortConditionEditorViewModel> SortConditions {
		get;
	}

	/// <summary>
	/// 登録されているソート条件の件数を取得します。
	/// </summary>
	public BindableReactiveProperty<int> ConditionCount {
		get;
	}

	/// <summary>
	/// ソート条件が0件であるかどうかを取得します。
	/// </summary>
	public BindableReactiveProperty<bool> IsEmpty {
		get;
	}

	/// <summary>
	/// ソート条件が選択されているかどうかを取得します。
	/// </summary>
	public BindableReactiveProperty<bool> HasCurrentCondition {
		get;
	}

	/// <summary>
	/// 条件未選択かつ0件の空状態メッセージを表示するかどうかを取得します。
	/// </summary>
	public BindableReactiveProperty<bool> ShowEmptyMessage {
		get;
	}

	/// <summary>
	/// 条件未選択かつ1件以上のときの案内メッセージを表示するかどうかを取得します。
	/// </summary>
	public BindableReactiveProperty<bool> ShowSelectConditionMessage {
		get;
	}

	/// <summary>
	/// ソート条件追加コマンドを取得します。
	/// </summary>
	public ReactiveCommand AddSortConditionCommand {
		get;
	}

	/// <summary>
	/// ソート条件削除コマンドを取得します。
	/// </summary>
	public ReactiveCommand<SortConditionEditorViewModel> RemoveSortConditionCommand {
		get;
	}

	/// <summary>
	/// 保存コマンドを取得します。
	/// </summary>
	public ReactiveCommand SaveCommand {
		get;
	}

	/// <summary>
	/// 適用コマンドを取得します。
	/// </summary>
	public ReactiveCommand ApplyCommand {
		get;
	}

	/// <summary>
	/// 読み込みコマンドを取得します。
	/// </summary>
	public ReactiveCommand LoadCommand {
		get;
	}

	/// <summary>
	/// 新しいソート条件を追加して選択状態にします。
	/// </summary>
	public void Add() {
		this._sortManager.AddCondition();
		this.CurrentCondition.Value = this.SortConditions.LastOrDefault();
	}

	/// <summary>
	/// 指定したソート条件を削除し、選択中の場合は隣接項目へ選択を移します。
	/// </summary>
	/// <param name="sortConditionViewModel">削除対象のソート条件 ViewModel。</param>
	public void Remove(SortConditionEditorViewModel sortConditionViewModel) {
		var list = (IList<SortConditionEditorViewModel>)this.SortConditions;
		var index = list.IndexOf(sortConditionViewModel);
		var wasSelected = ReferenceEquals(this.CurrentCondition.Value, sortConditionViewModel);
		this._sortManager.RemoveCondition(sortConditionViewModel.Model);

		if (wasSelected) {
			if (list.Count == 0) {
				this.CurrentCondition.Value = null;
			} else {
				var nextIndex = Math.Clamp(index, 0, list.Count - 1);
				this.CurrentCondition.Value = list[nextIndex];
			}
		}
	}

	/// <summary>
	/// 先頭のソート条件を選択状態として読み込みます。
	/// </summary>
	public void Load() {
		this.CurrentCondition.Value = this.SortConditions.FirstOrDefault();
	}

	/// <summary>
	/// 変更を永続化します。
	/// </summary>
	public void Save() {
		this._sortManager.Save();
	}
}