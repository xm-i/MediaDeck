using MediaDeck.Common.Base;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;

namespace MediaDeck.ViewModels.Thumbnails;

/// <summary>
/// 複数アイテムに対する一括サムネイル再生成ウィンドウの ViewModel。
/// 選択アイテム群のうち先頭と同じメディアタイプを持つものだけを抽出し、
/// 共通の <see cref="IBulkThumbnailConfigViewModel"/> を用いて順次再生成を行う。
/// </summary>
[Inject(InjectServiceLifetime.Transient)]
public class BulkThumbnailRegenerationViewModel : ViewModelBase {
	private readonly IMediaItemTypeService _mediaItemTypeService;
	private CancellationTokenSource? _cts;

	private readonly ObservableList<BulkThumbnailItemViewModel> _items = [];

	/// <summary>
	/// <see cref="BulkThumbnailRegenerationViewModel"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="mediaItemTypeService">メディアアイテム種別サービス</param>
	public BulkThumbnailRegenerationViewModel(IMediaItemTypeService mediaItemTypeService) {
		this._mediaItemTypeService = mediaItemTypeService;

		this.Items = this._items.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current).AddTo(this.CompositeDisposable);
		this.TargetMediaType = new BindableReactiveProperty<MediaType>(MediaType.Unknown).AddTo(this.CompositeDisposable);
		this.ConfigViewModel = new BindableReactiveProperty<IBulkThumbnailConfigViewModel?>().AddTo(this.CompositeDisposable);
		this.IsRunning = new BindableReactiveProperty<bool>(false).AddTo(this.CompositeDisposable);
		this.CompletedCount = new BindableReactiveProperty<int>(0).AddTo(this.CompositeDisposable);
		this.TotalCount = new BindableReactiveProperty<int>(0).AddTo(this.CompositeDisposable);

		this.ExecuteCommand = this.IsRunning
			.Select(running => !running)
			.ToReactiveCommand()
			.AddTo(this.CompositeDisposable);
		this.ExecuteCommand.SubscribeAwait(async (_, ct) => await this.ExecuteAsync(), AwaitOperation.Drop).AddTo(this.CompositeDisposable);

		this.CancelCommand = this.IsRunning.ToReactiveCommand().AddTo(this.CompositeDisposable);
		this.CancelCommand.Subscribe(_ => this.Cancel()).AddTo(this.CompositeDisposable);
	}

	/// <summary>
	/// 処理対象アイテム一覧 (左側ペイン用)。
	/// </summary>
	public INotifyCollectionChangedSynchronizedViewList<BulkThumbnailItemViewModel> Items {
		get;
	}

	/// <summary>
	/// 抽出された対象メディアタイプ。
	/// </summary>
	public BindableReactiveProperty<MediaType> TargetMediaType {
		get;
	}

	/// <summary>
	/// メディアタイプに応じた設定 ViewModel (右側ペイン用)。
	/// </summary>
	public BindableReactiveProperty<IBulkThumbnailConfigViewModel?> ConfigViewModel {
		get;
	}

	/// <summary>
	/// 実行中フラグ。
	/// </summary>
	public BindableReactiveProperty<bool> IsRunning {
		get;
	}

	/// <summary>
	/// 完了済み件数。
	/// </summary>
	public BindableReactiveProperty<int> CompletedCount {
		get;
	}

	/// <summary>
	/// 全件数。
	/// </summary>
	public BindableReactiveProperty<int> TotalCount {
		get;
	}

	/// <summary>
	/// 一括再生成実行コマンド。
	/// </summary>
	public ReactiveCommand ExecuteCommand {
		get;
	}

	/// <summary>
	/// 一括再生成キャンセルコマンド。
	/// </summary>
	public ReactiveCommand CancelCommand {
		get;
	}

	/// <summary>
	/// 選択アイテムから対象を抽出し、ウィンドウ初期状態をセットアップする。
	/// </summary>
	/// <param name="selectedItems">右クリック時の選択アイテム群。</param>
	public void Initialize(IReadOnlyList<IMediaItemViewModel> selectedItems) {
		foreach (var existing in this.Items) {
			existing.Dispose();
		}
		this._items.Clear();
		(this.ConfigViewModel.Value as IDisposable)?.Dispose();
		this.ConfigViewModel.Value = null;
		this.CompletedCount.Value = 0;
		this.TotalCount.Value = 0;
		if (selectedItems.Count == 0) {
			return;
		}
		var firstType = selectedItems[0].MediaType;
		this.TargetMediaType.Value = firstType;
		var filtered = selectedItems.Where(x => x.MediaType == firstType).ToList();
		foreach (var item in filtered) {
			this._items.Add(new BulkThumbnailItemViewModel(item));
		}
		this.TotalCount.Value = this._items.Count;
		this.ConfigViewModel.Value = this._mediaItemTypeService.CreateBulkThumbnailConfigViewModel(firstType);
	}

	private async Task ExecuteAsync() {
		if (this.ConfigViewModel.Value is not { } config) {
			return;
		}
		if (this._items.Count == 0) {
			return;
		}

		this._cts?.Dispose();
		this._cts = new CancellationTokenSource();
		var token = this._cts.Token;

		this.IsRunning.Value = true;
		this.CompletedCount.Value = 0;

		try {
			foreach (var item in this.Items) {
				item.Status.Value = BulkRegenerationStatus.Pending;
				item.ErrorMessage.Value = null;
			}

			foreach (var item in this.Items) {
				try {
					token.ThrowIfCancellationRequested();
					item.Status.Value = BulkRegenerationStatus.Processing;
					await config.ApplyToAsync(item.Target, token);
					item.Status.Value = BulkRegenerationStatus.Completed;
					this.CompletedCount.Value++;
				} catch (OperationCanceledException) {
					item.Status.Value = BulkRegenerationStatus.Pending;
					break;
				} catch (Exception ex) {
					item.Status.Value = BulkRegenerationStatus.Failed;
					item.ErrorMessage.Value = ex.Message;
					this.CompletedCount.Value++;
				}
			}
		} finally {
			this.IsRunning.Value = false;
		}
	}

	private void Cancel() {
		this._cts?.Cancel();
	}

	/// <inheritdoc/>
	protected override void Dispose(bool disposing) {
		if (disposing) {
			this._cts?.Cancel();
			this._cts?.Dispose();
			this._cts = null;
			(this.ConfigViewModel.Value as IDisposable)?.Dispose();
			foreach (var item in this.Items) {
				item.Dispose();
			}
		}
		base.Dispose(disposing);
	}
}