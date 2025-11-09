using System.ComponentModel;

using MediaDeck.Composition.Bases;
using MediaDeck.Models.Files.Sort;
using MediaDeck.Utils.Enums;

namespace MediaDeck.ViewModels.Sort;

public class SortConditionEditorViewModel : ViewModelBase {
	public SortConditionEditorViewModel(SortConditionEditor model) {
		this.Model = model;
		this.DisplayName = this.Model.DisplayName.ToBindableReactiveProperty(null!);
		this.DisplayName.Subscribe(x => {
			this.Model.DisplayName.Value = x;
		});

		this.SortItemCreators = this.Model.SortItemCreators.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);

		this.AddSortItemCommand.Subscribe(x => {
			if (x is not { } sortItemKey) {
				return;
			}
			this.Model.AddSortItem(new SortItemCreator(sortItemKey!, this.Direction.Value));
		}).AddTo(this.CompositeDisposable);
		this.RemoveSortItemCommand.Subscribe(this.Model.RemoveSortItem).AddTo(this.CompositeDisposable);
		this.CandidateSortItemKeys.Value = Enum.GetValues<SortItemKey>();
	}

	/// <summary>
	/// モデル
	/// </summary>
	public SortConditionEditor Model {
		get;
	}

	/// <summary>
	/// 表示名
	/// </summary>
	public BindableReactiveProperty<string> DisplayName {
		get;
	}

	/// <summary>
	/// ソート条件クリエイター
	/// </summary>
	public INotifyCollectionChangedSynchronizedViewList<SortItemCreator> SortItemCreators {
		get;
	}

	/// <summary>
	///　設定用ソート項目リスト
	/// </summary>
	public ReactiveProperty<SortItemKey[]> CandidateSortItemKeys {
		get;
	} = new ();

	/// <summary>
	/// 設定用ソート方向
	/// </summary>
	public BindableReactiveProperty<ListSortDirection> Direction {
		get;
	} = new (ListSortDirection.Ascending);

	/// <summary>
	/// ソート条件削除コマンド
	/// </summary>
	public ReactiveCommand<SortItemCreator> RemoveSortItemCommand {
		get;
	} = new ();

	/// <summary>
	/// ソート条件追加コマンド
	/// </summary>
	public ReactiveCommand<SortItemKey?> AddSortItemCommand {
		get;
	} = new ();
}
