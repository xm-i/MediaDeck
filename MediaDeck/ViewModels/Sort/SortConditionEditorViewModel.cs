using System.ComponentModel;

using MediaDeck.Composition.Bases;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Stores.State.Model.Objects;

namespace MediaDeck.ViewModels.Sort;

public class SortConditionEditorViewModel : ViewModelBase {
	public SortConditionEditorViewModel(SortObject model) {
		this.Model = model;
		this.DisplayName = this.Model.DisplayName.ToBindableReactiveProperty(null!);
		this.DisplayName.Subscribe(x => {
			this.Model.DisplayName.Value = x;
		});

		this.SortItemObjects = this.Model.SortItemObjects.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);

		this.AddSortItemCommand.Subscribe(x => {
			if (x is not { } sortItemKey) {
				return;
			}
			var si = this.Model.AddSortItemObject();
			si.SortItemKey = sortItemKey;
			si.Direction = this.Direction.Value;
		}).AddTo(this.CompositeDisposable);
		this.RemoveSortItemCommand.Subscribe(this.Model.RemoveSortItemObject).AddTo(this.CompositeDisposable);
		this.CandidateSortItemKeys.Value = Enum.GetValues<SortItemKey>();
	}

	/// <summary>
	/// モデル
	/// </summary>
	public SortObject Model {
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
	public INotifyCollectionChangedSynchronizedViewList<SortItemObject> SortItemObjects {
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
	public ReactiveCommand<SortItemObject> RemoveSortItemCommand {
		get;
	} = new ();

	/// <summary>
	/// ソート条件追加コマンド
	/// </summary>
	public ReactiveCommand<SortItemKey?> AddSortItemCommand {
		get;
	} = new ();
}
