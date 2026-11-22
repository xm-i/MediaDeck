using MediaDeck.Common.Base;
using MediaDeck.Common.Extensions;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Core.Models.Files.Filter;
using MediaDeck.ViewModels.Filters.FilterItemCreators;

namespace MediaDeck.ViewModels.Filters;

/// <summary>
/// フィルター管理画面の ViewModel クラス。
/// </summary>
[Inject(InjectServiceLifetime.Transient)]
public class FilterManagerViewModel : ViewModelBase {
	private readonly FilterManager _filterManager;

	/// <summary>
	/// <see cref="FilterManagerViewModel"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="filterManager">フィルターマネージャー。</param>
	/// <param name="stringProvider">多言語リソースプロバイダー。</param>
	/// <param name="tagsManager">タグマネージャー。</param>
	public FilterManagerViewModel(FilterManager filterManager, IStringProvider stringProvider, ITagsManager tagsManager) {
		this._filterManager = filterManager;

		this.CurrentCondition = new BindableReactiveProperty<FilteringConditionEditorViewModel?>().AddTo(this.CompositeDisposable);
		this.AddFilteringConditionCommand = new ReactiveCommand().AddTo(this.CompositeDisposable);
		this.RemoveFilteringConditionCommand = new ReactiveCommand<FilteringConditionEditorViewModel>().AddTo(this.CompositeDisposable);
		this.SaveCommand = new ReactiveCommand().AddTo(this.CompositeDisposable);
		this.ApplyCommand = new ReactiveCommand().AddTo(this.CompositeDisposable);
		this.SelectFilterCreatorCommand = new ReactiveCommand<IFilterCreatorViewModel>().AddTo(this.CompositeDisposable);

		this.FilteringConditions = filterManager.FilteringConditions
			.CreateViewWithDisposal(x => new FilteringConditionEditorViewModel(x, stringProvider))
			.ToNotifyCollectionChangedWithDisposal(SynchronizationContextCollectionEventDispatcher.Current)
			.AddTo(this.CompositeDisposable);

		this.ConditionCount = filterManager.FilteringConditions
			.ObserveCountChanged()
			.ToBindableReactiveProperty(filterManager.FilteringConditions.Count)
			.AddTo(this.CompositeDisposable);

		this.IsEmpty = this.ConditionCount
			.Select(c => c == 0)
			.ToBindableReactiveProperty(filterManager.FilteringConditions.Count == 0)
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

		this.TagFilterCreator = new TagFilterCreatorViewModel(this.CurrentCondition, stringProvider, tagsManager).AddTo(this.CompositeDisposable);
		this.FilePathFilterCreator = new FilePathFilterCreatorViewModel(this.CurrentCondition, stringProvider).AddTo(this.CompositeDisposable);
		this.MediaTypeFilterCreator = new MediaTypeFilterCreatorViewModel(this.CurrentCondition, stringProvider).AddTo(this.CompositeDisposable);
		this.RateFilterCreator = new RateFilterCreatorViewModel(this.CurrentCondition, stringProvider).AddTo(this.CompositeDisposable);
		this.ResolutionFilterCreator = new ResolutionFilterCreatorViewModel(this.CurrentCondition, stringProvider).AddTo(this.CompositeDisposable);
		this.LocationFilterCreator = new LocationFilterCreatorViewModel(this.CurrentCondition, stringProvider).AddTo(this.CompositeDisposable);
		this.FolderGroupFilterCreator = new FolderGroupFilterCreatorViewModel(this.CurrentCondition, stringProvider).AddTo(this.CompositeDisposable);
		this.ExistsFilterCreator = new ExistsFilterCreatorViewModel(this.CurrentCondition, stringProvider).AddTo(this.CompositeDisposable);

		this.FilterCreatorViewModels = [
			this.TagFilterCreator,
			this.FilePathFilterCreator,
			this.MediaTypeFilterCreator,
			this.RateFilterCreator,
			this.ResolutionFilterCreator,
			this.LocationFilterCreator,
			this.FolderGroupFilterCreator,
			this.ExistsFilterCreator
		];

		this.SelectedFilterCreatorViewModel = new BindableReactiveProperty<IFilterCreatorViewModel>(this.TagFilterCreator)
			.AddTo(this.CompositeDisposable);
		this.SelectedFilterCreatorKind = new BindableReactiveProperty<FilterCreatorKind>(FilterCreatorKind.Tag)
			.AddTo(this.CompositeDisposable);

		this.AddFilteringConditionCommand.Subscribe(_ => {
			this.Add();
		}).AddTo(this.CompositeDisposable);

		this.RemoveFilteringConditionCommand
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

		this.SelectFilterCreatorCommand.Subscribe(this.SelectFilterCreator).AddTo(this.CompositeDisposable);

		this.SelectFilterCreator(this.TagFilterCreator);
		this.CurrentCondition.Value = this.FilteringConditions.FirstOrDefault();
	}

	/// <summary>
	/// カレント条件を取得します。
	/// </summary>
	public BindableReactiveProperty<FilteringConditionEditorViewModel?> CurrentCondition {
		get;
	}

	/// <summary>
	/// フィルタリング条件一覧を取得します。
	/// </summary>
	public INotifyCollectionChangedSynchronizedViewList<FilteringConditionEditorViewModel> FilteringConditions {
		get;
	}

	/// <summary>
	/// 登録されているフィルター条件の件数を取得します。
	/// </summary>
	public BindableReactiveProperty<int> ConditionCount {
		get;
	}

	/// <summary>
	/// フィルター条件が0件であるかどうかを取得します。
	/// </summary>
	public BindableReactiveProperty<bool> IsEmpty {
		get;
	}

	/// <summary>
	/// フィルター条件が選択されているかどうかを取得します。
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
	/// フィルタリング条件追加コマンドを取得します。
	/// </summary>
	public ReactiveCommand AddFilteringConditionCommand {
		get;
	}

	/// <summary>
	/// フィルタリング条件削除コマンドを取得します。
	/// </summary>
	public ReactiveCommand<FilteringConditionEditorViewModel> RemoveFilteringConditionCommand {
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
	/// フィルター項目作成カテゴリを選択するコマンドを取得します。
	/// </summary>
	public ReactiveCommand<IFilterCreatorViewModel> SelectFilterCreatorCommand {
		get;
	}

	/// <summary>
	/// フィルター条件作成VMリストを取得します。
	/// </summary>
	public IReadOnlyList<IFilterCreatorViewModel> FilterCreatorViewModels {
		get;
	}

	/// <summary>
	/// 選択中フィルター条件作成VMを取得します。
	/// </summary>
	public BindableReactiveProperty<IFilterCreatorViewModel> SelectedFilterCreatorViewModel {
		get;
	}

	/// <summary>
	/// 選択中フィルター条件作成種別を取得します。
	/// </summary>
	public BindableReactiveProperty<FilterCreatorKind> SelectedFilterCreatorKind {
		get;
	}

	/// <summary>
	/// タグフィルター作成VMを取得します。
	/// </summary>
	public TagFilterCreatorViewModel TagFilterCreator {
		get;
	}

	/// <summary>
	/// ファイルパスフィルター作成VMを取得します。
	/// </summary>
	public FilePathFilterCreatorViewModel FilePathFilterCreator {
		get;
	}

	/// <summary>
	/// メディアタイプフィルター作成VMを取得します。
	/// </summary>
	public MediaTypeFilterCreatorViewModel MediaTypeFilterCreator {
		get;
	}

	/// <summary>
	/// 評価フィルター作成VMを取得します。
	/// </summary>
	public RateFilterCreatorViewModel RateFilterCreator {
		get;
	}

	/// <summary>
	/// 解像度フィルター作成VMを取得します。
	/// </summary>
	public ResolutionFilterCreatorViewModel ResolutionFilterCreator {
		get;
	}

	/// <summary>
	/// 座標フィルター作成VMを取得します。
	/// </summary>
	public LocationFilterCreatorViewModel LocationFilterCreator {
		get;
	}

	/// <summary>
	/// フォルダグループフィルター作成VMを取得します。
	/// </summary>
	public FolderGroupFilterCreatorViewModel FolderGroupFilterCreator {
		get;
	}

	/// <summary>
	/// ファイル存在フィルター作成VMを取得します。
	/// </summary>
	public ExistsFilterCreatorViewModel ExistsFilterCreator {
		get;
	}

	/// <summary>
	/// 指定したフィルター項目作成カテゴリを選択状態にします。
	/// </summary>
	/// <param name="creator">選択するフィルター項目作成VM。</param>
	public void SelectFilterCreator(IFilterCreatorViewModel? creator) {
		if (creator is null) {
			return;
		}
		foreach (var item in this.FilterCreatorViewModels) {
			item.IsSelected.Value = ReferenceEquals(item, creator);
		}
		this.SelectedFilterCreatorViewModel.Value = creator;
		this.SelectedFilterCreatorKind.Value = creator.Kind;
	}

	/// <summary>
	/// 新しいフィルター条件を追加して選択状態にします。
	/// </summary>
	public void Add() {
		this._filterManager.AddCondition();
		this.CurrentCondition.Value = this.FilteringConditions.LastOrDefault();
	}

	/// <summary>
	/// 指定したフィルター条件を削除し、選択中の場合は隣接項目へ選択を移します。
	/// </summary>
	/// <param name="filteringConditionViewModel">削除対象のフィルター条件 ViewModel。</param>
	public void Remove(FilteringConditionEditorViewModel filteringConditionViewModel) {
		var list = (IList<FilteringConditionEditorViewModel>)this.FilteringConditions;
		var index = list.IndexOf(filteringConditionViewModel);
		var wasSelected = ReferenceEquals(this.CurrentCondition.Value, filteringConditionViewModel);
		this._filterManager.RemoveCondition(filteringConditionViewModel.Model);

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
	/// 変更を永続化します。
	/// </summary>
	public void Save() {
		this._filterManager.Save();
	}
}