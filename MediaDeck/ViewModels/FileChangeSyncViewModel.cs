using MediaDeck.Composition.Bases;
using MediaDeck.Models.Services;
using System.Collections.Specialized;
using ObservableCollections;

namespace MediaDeck.ViewModels;

/// <summary>
/// ファイル変更のフィルター種類
/// </summary>
public enum FileChangeFilter {
	/// <summary>
	/// すべて
	/// </summary>
	All,

	/// <summary>
	/// 移動のみ
	/// </summary>
	Moved,

	/// <summary>
	/// 削除のみ
	/// </summary>
	Deleted
}

/// <summary>
/// ファイル変更同期確認ウィンドウのViewModel
/// </summary>
[Inject(InjectServiceLifetime.Transient)]
public class FileChangeSyncViewModel : ViewModelBase {
	private readonly FileChangeMonitorService _service;
	private readonly ISynchronizedView<FileChangeItem, FileChangeItem> _view;

	/// <summary>
	/// 表示対象の変更リスト
	/// </summary>
	public INotifyCollectionChangedSynchronizedViewList<FileChangeItem> Changes {
		get;
	}

	/// <summary>
	/// フィルターの種類
	/// </summary>
	public BindableReactiveProperty<FileChangeFilter?> FilterType {
		get;
	}

	/// <summary>
	/// 現在の表示件数
	/// </summary>
	public BindableReactiveProperty<int> FilteredCount {
		get;
	}

	/// <summary>
	/// 表示中の全アイテムを反映するコマンド
	/// </summary>
	public ReactiveCommand ApplyAllCommand {
		get;
	}

	/// <summary>
	/// 表示中の全アイテムを無視するコマンド
	/// </summary>
	public ReactiveCommand DiscardAllCommand {
		get;
	}

	/// <summary>
	/// 選択可能なフィルターのリスト
	/// </summary>
	public FileChangeFilter[] Filters { get; } = [
		FileChangeFilter.All,
		FileChangeFilter.Moved,
		FileChangeFilter.Deleted
	];

	/// <summary>
	/// 単一アイテムを反映するコマンド
	/// </summary>
	public ReactiveCommand<FileChangeItem> ApplySingleCommand {
		get;
	}

	/// <summary>
	/// 単一アイテムを無視するコマンド
	/// </summary>
	public ReactiveCommand<FileChangeItem> DiscardSingleCommand {
		get;
	}

	public FileChangeSyncViewModel(FileChangeMonitorService service) {
		this._service = service;

		this.FilterType = new BindableReactiveProperty<FileChangeFilter?>(FileChangeFilter.All).AddTo(this.CompositeDisposable);
		this.FilteredCount = new BindableReactiveProperty<int>(0).AddTo(this.CompositeDisposable);

		this._view = this._service.UnprocessedChanges.CreateView(x => x);
		this._view.AddTo(this.CompositeDisposable);
		this.Changes = this._view.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);

		// フィルター変更の監視
		this.FilterType.Where(x => x.HasValue).Subscribe(f => {
			this._view.AttachFilter(x => f switch {
				FileChangeFilter.All => true,
				FileChangeFilter.Moved => x.ChangeType == FileChangeType.Moved,
				FileChangeFilter.Deleted => x.ChangeType == FileChangeType.Deleted,
				_ => true
			});
			this.FilteredCount.Value = this._view.Count;
		}).AddTo(this.CompositeDisposable);

		// ビュー内容変更の監視（削除などが実行された際に件数を更新）
		// ViewChanged は ref struct を含むため、CollectionStateChanged を使用する
		Observable.FromEvent<Action<NotifyCollectionChangedAction>, NotifyCollectionChangedAction>(
			h => h,
			h => this._view.CollectionStateChanged += h,
			h => this._view.CollectionStateChanged -= h)
			.Subscribe(_ => this.FilteredCount.Value = this._view.Count)
			.AddTo(this.CompositeDisposable);

		this.ApplyAllCommand = new ReactiveCommand();
		this.ApplyAllCommand.SubscribeAwait(async (_, ct) => {
			// 現在表示されているアイテムのみを取得
			var items = this._view.Select(x => x).ToArray();
			await this._service.ApplyChangesAsync(items, false);
		}).AddTo(this.CompositeDisposable);

		this.DiscardAllCommand = new ReactiveCommand();
		this.DiscardAllCommand.Subscribe(_ => {
			// 現在表示されているアイテムのみを取得
			var items = this._view.Select(x => x).ToArray();
			this._service.DiscardChanges(items);
		}).AddTo(this.CompositeDisposable);

		this.ApplySingleCommand = new ReactiveCommand<FileChangeItem>();
		this.ApplySingleCommand.SubscribeAwait(async (item, ct) => {
			await this._service.ApplyChangesAsync([item], false);
		}).AddTo(this.CompositeDisposable);

		this.DiscardSingleCommand = new ReactiveCommand<FileChangeItem>();
		this.DiscardSingleCommand.Subscribe(item => {
			this._service.DiscardChanges([item]);
		}).AddTo(this.CompositeDisposable);
	}
}
