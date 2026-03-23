using MediaDeck.Composition.Bases;
using MediaDeck.Models.Services;
using System.Collections.Specialized;
using System.Linq;
using System;
using System.Threading.Tasks;
using R3;
using ObservableCollections;

namespace MediaDeck.ViewModels;

/// <summary>
/// ファイル変更のフィルター種類を定義します。
/// </summary>
public enum FileChangeFilter {
	/// <summary>すべて表示</summary>
	All,

	/// <summary>移動のみ</summary>
	Moved,

	/// <summary>削除のみ</summary>
	Deleted,

	/// <summary>名前変更のみ</summary>
	Renamed,

	/// <summary>追加のみ</summary>
	Added
}

/// <summary>
/// ファイル変更の同期/反映を管理する画面のViewModelです。
/// </summary>
[Inject(InjectServiceLifetime.Transient)]
public partial class FileChangeSyncViewModel : ViewModelBase {
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
		FileChangeFilter.Deleted,
		FileChangeFilter.Renamed,
		FileChangeFilter.Added
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

	/// <summary>
	/// FileChangeSyncViewModelクラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="service">ファイル変更監視サービス</param>
	public FileChangeSyncViewModel(FileChangeMonitorService service) {
		this._service = service;

		this.FilterType = new BindableReactiveProperty<FileChangeFilter?>(FileChangeFilter.All).AddTo(this.CompositeDisposable);
		this.FilteredCount = new BindableReactiveProperty<int>(0).AddTo(this.CompositeDisposable);

		this._view = this._service.Tracker.UnprocessedChanges.CreateView(x => x);
		this._view.AddTo(this.CompositeDisposable);
		this.Changes = this._view.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);

		// フィルター変更の監視。選択されたフィルターに応じて表示リストを絞り込みます。
		this.FilterType.Where(x => x.HasValue).Subscribe(f => {
			this._view.AttachFilter(x => f switch {
				FileChangeFilter.All => true,
				FileChangeFilter.Moved => x.ChangeType == FileChangeType.Moved,
				FileChangeFilter.Deleted => x.ChangeType == FileChangeType.Deleted,
				FileChangeFilter.Renamed => x.ChangeType == FileChangeType.Renamed,
				FileChangeFilter.Added => x.ChangeType == FileChangeType.Added,
				_ => true
			});
			this.FilteredCount.Value = this._view.Count;
		}).AddTo(this.CompositeDisposable);

		// ビュー内容変更の監視（削除などが実行された際に件数を更新）
		Observable.FromEvent<Action<NotifyCollectionChangedAction>, NotifyCollectionChangedAction>(
			h => h,
			h => this._view.CollectionStateChanged += h,
			h => this._view.CollectionStateChanged -= h)
			.Subscribe(_ => this.FilteredCount.Value = this._view.Count)
			.AddTo(this.CompositeDisposable);

		// すべて反映コマンド
		this.ApplyAllCommand = new ReactiveCommand();
		this.ApplyAllCommand.SubscribeAwait(async (_, ct) => {
			// 現在表示（フィルタリング）されているアイテムのみを対象に反映を実行
			var items = this._view.Select(x => x).ToArray();
			await this._service.ApplyChangesAsync(items, false);
		}).AddTo(this.CompositeDisposable);

		// すべて無視コマンド
		this.DiscardAllCommand = new ReactiveCommand();
		this.DiscardAllCommand.Subscribe(_ => {
			// 現在表示されているアイテムのみを対象に無視を実行
			var items = this._view.Select(x => x).ToArray();
			this._service.DiscardChanges(items);
		}).AddTo(this.CompositeDisposable);

		// 単一反映コマンド
		this.ApplySingleCommand = new ReactiveCommand<FileChangeItem>();
		this.ApplySingleCommand.SubscribeAwait(async (item, ct) => {
			await this._service.ApplyChangesAsync(new[] { item }, false);
		}).AddTo(this.CompositeDisposable);

		// 単一無視コマンド
		this.DiscardSingleCommand = new ReactiveCommand<FileChangeItem>();
		this.DiscardSingleCommand.Subscribe(item => {
			this._service.DiscardChanges(new[] { item });
		}).AddTo(this.CompositeDisposable);
	}
}
