using MediaDeck.Common.Base;
using MediaDeck.Common.Extensions;
using MediaDeck.Composition.Database;
using MediaDeck.Composition.Stores.Config.Model;

using Microsoft.Extensions.Logging;

namespace MediaDeck.Core.Services.FileChangeMonitor;

/// <summary>
/// 未処理のファイル変更リストの管理およびDBとファイルシステム実態の差分計算（Reconcile）のスケジューリングを担うクラスです。
/// </summary>
[Inject(InjectServiceLifetime.Singleton)]
public class FileChangeTracker : ServiceBase {
	private const int MaxFileReadRetryCount = 5;
	private const int MaxDbGenerationRetryCount = 3;

	private readonly ILogger<FileChangeTracker> _logger;
	private readonly FileChangeReconciler _reconciler;
	private readonly Lock _lock = new();
	private readonly SemaphoreSlim _processLock = new(1, 1);
	private readonly CancellationTokenSource _disposeCts = new();

	private readonly HashSet<string> _dirtyPaths = new(StringComparer.OrdinalIgnoreCase);
	private readonly List<RenameOperation> _renameHistory = [];
	private readonly Dictionary<string, DateTime> _lastWriteTimes = new(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, int> _retryCounts = new(StringComparer.OrdinalIgnoreCase);
	private readonly List<(long Generation, FileChangeItem Item)> _removedDuringReconcile = [];
	private readonly HashSet<string> _dbUpdatedPathsDuringReconcile = new(StringComparer.OrdinalIgnoreCase);
	private CancellationTokenSource? _debounceCts;
	private long _reconcileGeneration;
	private long _dbMutationGeneration;
	private bool _isReconciling;
	private bool _isDisposed;

	/// <summary>
	/// 未処理の変更リスト（差分確定済みのアイテムのみ保持）
	/// </summary>
	public ObservableList<FileChangeItem> UnprocessedChanges {
		get;
	} = new();

	/// <summary>
	/// 連続したファイル変更や書き込みの安定を待つデバウンス時間
	/// </summary>
	public TimeSpan StabilityDelay {
		get;
		set;
	} = TimeSpan.FromMilliseconds(500);

	/// <summary>
	/// <see cref="FileChangeTracker"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="dbFactory">DBコンテキストファクトリー</param>
	/// <param name="logger">ロガー</param>
	/// <param name="config">設定モデル</param>
	public FileChangeTracker(IDbContextFactory<MediaDeckDbContext> dbFactory, ILogger<FileChangeTracker> logger, ConfigModel config) {
		this._logger = logger;
		this._reconciler = new FileChangeReconciler(
			dbFactory,
			config,
			logger,
			this.TryConsumeRetrySlot,
			this.ResetRetryCount);
	}

	/// <summary>
	/// ファイル・フォルダ作成イベントを通知します。
	/// </summary>
	/// <param name="path">作成されたファイルまたはフォルダのパス</param>
	public void OnCreated(string path) {
		var normPath = FileChangePathUtility.NormalizePath(path);
		if (string.IsNullOrEmpty(normPath)) {
			return;
		}

		this._logger.LogDebug("File created (Dirty): {Path}", normPath);
		lock (this._lock) {
			if (this._isDisposed) {
				return;
			}
			this._dirtyPaths.Add(normPath);
			this._lastWriteTimes[normPath] = DateTime.UtcNow;
			this._retryCounts.Remove(normPath);
		}
		this.ScheduleReconcile();
	}

	/// <summary>
	/// ファイル・フォルダ削除イベントを通知します。
	/// </summary>
	/// <param name="path">削除されたファイルまたはフォルダのパス</param>
	public void OnDeleted(string path) {
		var normPath = FileChangePathUtility.NormalizePath(path);
		if (string.IsNullOrEmpty(normPath)) {
			return;
		}

		this._logger.LogDebug("File deleted (Dirty): {Path}", normPath);
		lock (this._lock) {
			if (this._isDisposed) {
				return;
			}
			this._dirtyPaths.Add(normPath);
			this._lastWriteTimes.Remove(normPath);
			this._retryCounts.Remove(normPath);
		}
		this.ScheduleReconcile();
	}

	/// <summary>
	/// ファイル・フォルダ名変更（同一ドライブ内移動）イベントを通知します。
	/// </summary>
	/// <param name="oldPath">変更前のパス</param>
	/// <param name="newPath">変更後のパス</param>
	public void OnRenamed(string oldPath, string newPath) {
		var normOld = FileChangePathUtility.NormalizePath(oldPath);
		var normNew = FileChangePathUtility.NormalizePath(newPath);
		if (string.IsNullOrEmpty(normOld) || string.IsNullOrEmpty(normNew)) {
			return;
		}

		this._logger.LogDebug("File renamed (Dirty): {OldPath} -> {NewPath}", normOld, normNew);
		lock (this._lock) {
			if (this._isDisposed) {
				return;
			}
			this._dirtyPaths.Add(normOld);
			this._dirtyPaths.Add(normNew);
			if (this._lastWriteTimes.Remove(normOld, out var writeTime)) {
				this._lastWriteTimes[normNew] = writeTime;
			} else {
				this._lastWriteTimes[normNew] = DateTime.UtcNow;
			}
			this._retryCounts.Remove(normOld);
			this._retryCounts.Remove(normNew);

			if (!string.Equals(normOld, normNew, StringComparison.Ordinal)) {
				if (this._renameHistory.Count == 0
					|| !string.Equals(this._renameHistory[^1].OldPath, normOld, StringComparison.Ordinal)
					|| !string.Equals(this._renameHistory[^1].NewPath, normNew, StringComparison.Ordinal)) {
					this._renameHistory.Add(new RenameOperation(normOld, normNew));
				}
			}
		}
		this.ScheduleReconcile();
	}

	/// <summary>
	/// ファイル変更イベントを通知します。
	/// </summary>
	/// <param name="path">変更されたファイルのパス</param>
	public void OnChanged(string path) {
		var normPath = FileChangePathUtility.NormalizePath(path);
		if (string.IsNullOrEmpty(normPath)) {
			return;
		}

		this._logger.LogDebug("File changed (Dirty): {Path}", normPath);
		bool shouldSchedule;
		lock (this._lock) {
			if (this._isDisposed) {
				return;
			}
			shouldSchedule = this._dirtyPaths.Contains(normPath)
				|| this._dirtyPaths.Any(d => FileChangePathUtility.IsSelfOrDescendant(normPath, d));
			if (shouldSchedule) {
				this._lastWriteTimes[normPath] = DateTime.UtcNow;
			}
		}

		if (shouldSchedule) {
			this.ScheduleReconcile();
		}
	}

	/// <summary>
	/// 蓄積された変更トリガーについて、DBの状態とディスク上の実態を比較して未処理変更リストを更新します。
	/// </summary>
	public async Task ProcessPendingChangesAsync() {
		await this.ProcessPendingChangesCoreAsync(respectStabilityDelay: false).ConfigureAwait(false);
	}

	/// <summary>
	/// 指定されたアイテムをリストから一括削除します（無視操作など、DB変更を伴わない除外用）。
	/// </summary>
	/// <param name="items">削除対象のアイテム一覧</param>
	public void RemoveItems(IEnumerable<FileChangeItem> items) {
		var itemList = items.ToList();
		if (itemList.Count == 0) {
			return;
		}

		lock (this._lock) {
			var targetElements = this.UnprocessedChanges
				.Where(x => itemList.Any(i => IsSameChangeItem(i, x)))
				.ToList();
			if (targetElements.Count > 0) {
				this.UnprocessedChanges.RemoveRange(targetElements);
			}
			if (this._isReconciling) {
				foreach (var item in itemList) {
					this._removedDuringReconcile.Add((this._reconcileGeneration, item));
				}
			}
		}
	}

	/// <summary>
	/// DBへ反映済みの変更アイテムをリストから削除し、反映中に別パスへ再変更されていた同一IDアイテムの基準パスを更新します。
	/// </summary>
	/// <param name="appliedItems">DBおよび登録キューへ反映した変更アイテム一覧</param>
	/// <param name="updatedPathsById">DB上で新しいパスへ更新されたメディアアイテムIDと更新後パスの対応</param>
	/// <param name="deletedIds">DBから削除されたメディアアイテムIDの集合</param>
	public void NotifyChangesApplied(
		IEnumerable<FileChangeItem> appliedItems,
		IReadOnlyDictionary<long, string> updatedPathsById,
		IReadOnlyCollection<long> deletedIds) {
		var itemList = appliedItems.ToList();
		var deletedIdSet = deletedIds as ISet<long> ?? deletedIds.ToHashSet();
		var needsReschedule = false;

		lock (this._lock) {
			if (this._isDisposed) {
				return;
			}

			this.RecordAppliedMutationsLocked(itemList, updatedPathsById, deletedIdSet);
			needsReschedule = this.UpdateUnprocessedChangesOnApplyLocked(itemList, deletedIdSet, updatedPathsById);
		}

		if (needsReschedule) {
			this.ScheduleReconcile();
		}
	}

	/// <summary>
	/// 反映済みの変更およびDB更新パスを進行中の差分計算世代向けに記録します。
	/// </summary>
	/// <param name="itemList">反映した変更アイテム一覧</param>
	/// <param name="updatedPathsById">DB上でパス更新されたアイテムの対応表</param>
	/// <param name="deletedIdSet">DBから削除されたメディアアイテムID集合</param>
	private void RecordAppliedMutationsLocked(
		List<FileChangeItem> itemList,
		IReadOnlyDictionary<long, string> updatedPathsById,
		ISet<long> deletedIdSet) {
		if (updatedPathsById.Count > 0 || deletedIdSet.Count > 0) {
			this._dbMutationGeneration++;
		}

		if (!this._isReconciling) {
			return;
		}

		foreach (var item in itemList) {
			this._removedDuringReconcile.Add((this._reconcileGeneration, item));
			if (!string.IsNullOrEmpty(item.OldPath)) {
				this._dbUpdatedPathsDuringReconcile.Add(FileChangePathUtility.NormalizePath(item.OldPath));
			}
			if (!string.IsNullOrEmpty(item.NewPath)) {
				this._dbUpdatedPathsDuringReconcile.Add(FileChangePathUtility.NormalizePath(item.NewPath));
			}
		}
		foreach (var updatedPath in updatedPathsById.Values) {
			var normUpdated = FileChangePathUtility.NormalizePath(updatedPath);
			if (!string.IsNullOrEmpty(normUpdated)) {
				this._dbUpdatedPathsDuringReconcile.Add(normUpdated);
			}
		}
	}

	/// <summary>
	/// 反映結果に応じて <see cref="UnprocessedChanges"/> を更新し、再評価が必要かを返します。
	/// </summary>
	/// <param name="itemList">反映した変更アイテム一覧</param>
	/// <param name="deletedIdSet">削除されたメディアアイテムID集合</param>
	/// <param name="updatedPathsById">DB上でパス更新されたアイテムの対応表</param>
	/// <returns>再評価のスケジュールが必要な場合は true</returns>
	private bool UpdateUnprocessedChangesOnApplyLocked(
		List<FileChangeItem> itemList,
		ISet<long> deletedIdSet,
		IReadOnlyDictionary<long, string> updatedPathsById) {
		var needsReschedule = false;
		var toRemove = new List<FileChangeItem>();
		var toAdd = new List<FileChangeItem>();

		foreach (var existing in this.UnprocessedChanges) {
			if (itemList.Any(i => IsSameChangeItem(i, existing))) {
				toRemove.Add(existing);
				continue;
			}
			if (!existing.MediaItemId.HasValue) {
				continue;
			}

			var id = existing.MediaItemId.Value;
			if (deletedIdSet.Contains(id)) {
				// DBレコードが既に削除された後に別操作へ変わっていた場合は、最新パスで再評価する
				toRemove.Add(existing);
				if (!string.IsNullOrEmpty(existing.NewPath)) {
					this._dirtyPaths.Add(FileChangePathUtility.NormalizePath(existing.NewPath));
					needsReschedule = true;
				}
				continue;
			}

			if (!updatedPathsById.TryGetValue(id, out var newDbPath)) {
				continue;
			}

			// 反映中に B -> C 等へ再変更されていた場合は、基準パス(OldPath)を最新のDBパス(B)へ合わせる
			var normNewDbPath = FileChangePathUtility.NormalizePath(newDbPath);
			var normExistingOldPath = FileChangePathUtility.NormalizePath(existing.OldPath);
			if (string.Equals(normExistingOldPath, normNewDbPath, StringComparison.Ordinal)) {
				continue;
			}

			toRemove.Add(existing);
			var normExistingNewPath = FileChangePathUtility.NormalizePath(existing.NewPath);
			if (existing.ChangeType is FileChangeType.Renamed or FileChangeType.Moved
				&& string.Equals(normNewDbPath, normExistingNewPath, StringComparison.Ordinal)) {
				// DBの更新後パスと一致したため差分なし
				continue;
			}
			toAdd.Add(CreateRebasedItem(existing, newDbPath));
		}

		if (toRemove.Count > 0) {
			this.UnprocessedChanges.RemoveRange(toRemove);
		}
		if (toAdd.Count > 0) {
			this.UnprocessedChanges.AddRange(toAdd);
		}

		return needsReschedule;
	}

	/// <summary>
	/// 基準パスを最新の DB パスへ差し替えた <see cref="FileChangeItem"/> を生成します。
	/// </summary>
	/// <param name="existing">元の変更アイテム</param>
	/// <param name="newDbPath">更新後の DB パス</param>
	/// <returns>生成された変更アイテム</returns>
	private static FileChangeItem CreateRebasedItem(FileChangeItem existing, string newDbPath) {
		return new FileChangeItem {
			MediaItemId = existing.MediaItemId,
			ChangeType = existing.ChangeType,
			OldPath = newDbPath,
			NewPath = existing.NewPath,
			FileSize = existing.FileSize,
			OldHash = existing.OldHash,
			NewHash = existing.NewHash,
			IsPending = false
		};
	}

	/// <summary>
	/// デバウンス後に差分計算処理を実行するようスケジュールします。
	/// </summary>
	private void ScheduleReconcile() {
		TimeSpan delay;
		CancellationToken token;
		lock (this._lock) {
			if (this._isDisposed) {
				return;
			}
			delay = this.StabilityDelay;
			token = this.ResetDebounceTokenLocked();
		}
		this.LaunchDebouncedReconcile(delay, token);
	}

	/// <summary>
	/// 既存のデバウンストークンをキャンセル・破棄し、新しいトークンを発行します。
	/// </summary>
	/// <returns>新しく発行されたキャンセルトークン</returns>
	private CancellationToken ResetDebounceTokenLocked() {
		this._debounceCts?.Cancel();
		this._debounceCts?.Dispose();
		this._debounceCts = new CancellationTokenSource();
		return this._debounceCts.Token;
	}

	/// <summary>
	/// 指定された遅延時間の後に差分計算を実行する非同期タスクを起動します。
	/// </summary>
	/// <param name="delay">待機するデバウンス時間</param>
	/// <param name="token">キャンセルトークン</param>
	private void LaunchDebouncedReconcile(TimeSpan delay, CancellationToken token) {
		if (delay > TimeSpan.Zero) {
			_ = this.RunDebouncedReconcileAsync(delay, token);
		} else {
			_ = Task.Run(() => this.RunDebouncedReconcileAsync(delay, token), token);
		}
	}

	/// <summary>
	/// デバウンス待機後に未処理の変更を差分計算します。
	/// </summary>
	/// <param name="delay">待機するデバウンス時間</param>
	/// <param name="token">キャンセルトークン</param>
	private async Task RunDebouncedReconcileAsync(TimeSpan delay, CancellationToken token) {
		try {
			if (delay > TimeSpan.Zero) {
				await Task.Delay(delay, token).ConfigureAwait(false);
			}
			if (!token.IsCancellationRequested) {
				await this.ProcessPendingChangesCoreAsync(respectStabilityDelay: true).ConfigureAwait(false);
			}
		} catch (OperationCanceledException) {
			// 新しい変更イベントによりデバウンスタイマーが更新された場合は無視
		} catch (ObjectDisposedException) {
			// 破棄処理とのタイミング競合は無視
		} catch (Exception ex) {
			this._logger.LogError(ex, "Error while reconciling file changes");
		}
	}

	/// <summary>
	/// 蓄積された未処理トリガーのスナップショットを取得し、リトライ込みで差分計算を実行・反映します。
	/// </summary>
	/// <param name="respectStabilityDelay">最終書き込み時刻からの安定待機時間を考慮するかどうか</param>
	private async Task ProcessPendingChangesCoreAsync(bool respectStabilityDelay) {
		var disposeToken = this.GetDisposeTokenIfAlive();
		if (disposeToken is not { } token) {
			return;
		}

		try {
			await this._processLock.WaitAsync(token).ConfigureAwait(false);
		} catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException) {
			return;
		}

		long currentReconcileGeneration = 0;
		var enteredReconciling = false;

		try {
			if (!this.TryPrepareReconcileBatch(
				respectStabilityDelay,
				out var batch,
				out var rescheduleDelay,
				out var rescheduleToken) || batch is null) {
				if (rescheduleDelay.HasValue) {
					this.LaunchDebouncedReconcile(rescheduleDelay.Value, rescheduleToken);
				}
				return;
			}

			currentReconcileGeneration = batch.ReconcileGeneration;
			enteredReconciling = true;

			var committed = await this.ExecuteReconcileWithRetriesAsync(batch, token).ConfigureAwait(false);
			if (!committed) {
				this.RestoreFailedBatch(batch.DirtyPaths, batch.RenameHistory);
			}
		} catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException) {
			// Dispose 時のキャンセルは無視
		} finally {
			if (enteredReconciling) {
				lock (this._lock) {
					this._isReconciling = false;
					this._removedDuringReconcile.RemoveAll(x => x.Generation <= currentReconcileGeneration);
					this._dbUpdatedPathsDuringReconcile.Clear();
				}
			}
			try {
				this._processLock.Release();
			} catch (ObjectDisposedException) {
				// Dispose との競合は無視
			}
		}
	}

	/// <summary>
	/// インスタンスが破棄されていない場合に破棄連動キャンセルトークンを取得します。
	/// </summary>
	/// <returns>破棄されていない場合はキャンセルトークン、破棄済みの場合は null</returns>
	private CancellationToken? GetDisposeTokenIfAlive() {
		lock (this._lock) {
			return this._isDisposed ? null : this._disposeCts.Token;
		}
	}

	/// <summary>
	/// 蓄積された変更トリガーから今回の差分計算バッチを切り出します。安定待機が必要な場合は再スケジュール情報を返します。
	/// </summary>
	/// <param name="respectStabilityDelay">安定待機時間を考慮するかどうか</param>
	/// <param name="batch">切り出された差分計算バッチ</param>
	/// <param name="rescheduleDelay">再スケジュールが必要な場合の遅延時間</param>
	/// <param name="rescheduleToken">再スケジュール用のキャンセルトークン</param>
	/// <returns>差分計算を実行できる場合は true</returns>
	private bool TryPrepareReconcileBatch(
		bool respectStabilityDelay,
		out ReconcileBatch? batch,
		out TimeSpan? rescheduleDelay,
		out CancellationToken rescheduleToken) {
		batch = null;
		rescheduleDelay = null;
		rescheduleToken = default;

		lock (this._lock) {
			if (this._isDisposed || this._dirtyPaths.Count == 0) {
				return false;
			}

			if (respectStabilityDelay && this.StabilityDelay > TimeSpan.Zero) {
				var remaining = this.CalculateRemainingStabilityDelayLocked();
				if (remaining > TimeSpan.Zero) {
					rescheduleDelay = remaining + TimeSpan.FromMilliseconds(15);
					rescheduleToken = this.ResetDebounceTokenLocked();
					return false;
				}
			}

			var dirtyPaths = new HashSet<string>(this._dirtyPaths, StringComparer.OrdinalIgnoreCase);
			var renameHistory = new List<RenameOperation>(this._renameHistory);

			this._dirtyPaths.Clear();
			this._renameHistory.Clear();
			this._lastWriteTimes.Clear();
			this._removedDuringReconcile.Clear();
			this._dbUpdatedPathsDuringReconcile.Clear();

			var reconcileGeneration = ++this._reconcileGeneration;
			var startDbGeneration = this._dbMutationGeneration;
			this._isReconciling = true;

			this.SplitExistingItemsLocked(dirtyPaths, renameHistory, out var scopedExistingItems, out var untouchedExistingItems);
			batch = new ReconcileBatch(
				dirtyPaths,
				renameHistory,
				scopedExistingItems,
				untouchedExistingItems,
				reconcileGeneration,
				startDbGeneration);
			return true;
		}
	}

	/// <summary>
	/// 現在保持している変更トリガーに対して必要な追加の安定待機時間を算出します。
	/// </summary>
	/// <returns>追加で待機すべき残り時間</returns>
	private TimeSpan CalculateRemainingStabilityDelayLocked() {
		var minRequiredTime = DateTime.UtcNow - this.StabilityDelay;
		var remaining = TimeSpan.Zero;
		foreach (var kvp in this._lastWriteTimes) {
			if (kvp.Value > minRequiredTime &&
				(this._dirtyPaths.Contains(kvp.Key) || this._dirtyPaths.Any(d => FileChangePathUtility.IsSelfOrDescendant(kvp.Key, d)))) {
				var diff = kvp.Value - minRequiredTime;
				if (diff > remaining) {
					remaining = diff;
				}
			}
		}
		return remaining;
	}

	/// <summary>
	/// DB更新の競合を検出しつつ、最大リトライ回数まで差分計算とコミットを繰り返します。
	/// </summary>
	/// <param name="batch">差分計算バッチ</param>
	/// <param name="disposeToken">キャンセルトークン</param>
	/// <returns>結果を確定できた場合は true</returns>
	private async Task<bool> ExecuteReconcileWithRetriesAsync(ReconcileBatch batch, CancellationToken disposeToken) {
		for (var attempt = 0; attempt <= MaxDbGenerationRetryCount; attempt++) {
			disposeToken.ThrowIfCancellationRequested();

			var input = new ReconcileInput(
				batch.DirtyPaths,
				batch.RenameHistory,
				batch.ScopedExistingItems,
				batch.UntouchedExistingItems);
			var result = await this._reconciler.ReconcileAsync(input, disposeToken).ConfigureAwait(false);

			var committed = this.TryCommitReconciledItems(
				batch.ScopedExistingItems,
				result.ReconciledItems,
				result.UpgradedUntouchedItems,
				batch.ReconcileGeneration,
				batch.StartDbGeneration);

			if (committed) {
				if (result.RetryPaths.Count > 0 || result.RetryRenames.Count > 0) {
					this.ScheduleRetryItems(result.RetryPaths, result.RetryRenames);
				}
				return true;
			}

			if (attempt < MaxDbGenerationRetryCount && !this.RefreshBatchOnDbMutation(batch)) {
				break;
			}
		}

		return false;
	}

	/// <summary>
	/// 差分計算中に割り込んだ DB 更新パスを取り込み、再評価用にバッチ状態を最新化します。
	/// </summary>
	/// <param name="batch">更新対象の差分計算バッチ</param>
	/// <returns>破棄されておらず再評価を続行できる場合は true</returns>
	private bool RefreshBatchOnDbMutation(ReconcileBatch batch) {
		lock (this._lock) {
			if (this._isDisposed) {
				return false;
			}
			foreach (var updatedPath in this._dbUpdatedPathsDuringReconcile) {
				batch.DirtyPaths.Add(updatedPath);
			}
			this._dbUpdatedPathsDuringReconcile.Clear();
			batch.StartDbGeneration = this._dbMutationGeneration;
			this.SplitExistingItemsLocked(
				batch.DirtyPaths,
				batch.RenameHistory,
				out var scopedExistingItems,
				out var untouchedExistingItems);
			batch.ScopedExistingItems = scopedExistingItems;
			batch.UntouchedExistingItems = untouchedExistingItems;
			return true;
		}
	}

	/// <summary>
	/// ロック中等で読み取れなかったファイルパスをリトライキューへ戻し、再実行をスケジュールします。
	/// </summary>
	/// <param name="retryPaths">再試行対象のパス一覧</param>
	/// <param name="retryRenames">再試行時に引き継ぐリネーム操作一覧</param>
	private void ScheduleRetryItems(List<string> retryPaths, List<RenameOperation> retryRenames) {
		TimeSpan retryDelay;
		CancellationToken retryToken;
		lock (this._lock) {
			if (this._isDisposed) {
				return;
			}
			foreach (var retryPath in retryPaths) {
				this._dirtyPaths.Add(retryPath);
				this._lastWriteTimes[retryPath] = DateTime.UtcNow;
			}
			if (retryRenames.Count > 0) {
				this._renameHistory.InsertRange(0, retryRenames);
			}
			retryDelay = this.StabilityDelay > TimeSpan.Zero ? this.StabilityDelay : TimeSpan.FromMilliseconds(200);
			retryToken = this.ResetDebounceTokenLocked();
		}
		this.LaunchDebouncedReconcile(retryDelay, retryToken);
	}

	/// <summary>
	/// コミットを完了できなかったバッチのトリガーを未処理キューへ戻し、再実行をスケジュールします。
	/// </summary>
	/// <param name="dirtyPaths">戻すダーティパス集合</param>
	/// <param name="renameHistory">戻すリネーム履歴</param>
	private void RestoreFailedBatch(HashSet<string> dirtyPaths, List<RenameOperation> renameHistory) {
		TimeSpan retryDelay;
		CancellationToken retryToken;
		lock (this._lock) {
			if (this._isDisposed) {
				return;
			}
			foreach (var p in dirtyPaths) {
				this._dirtyPaths.Add(p);
			}
			this._renameHistory.InsertRange(0, renameHistory);
			retryDelay = this.StabilityDelay > TimeSpan.Zero ? this.StabilityDelay : TimeSpan.FromMilliseconds(200);
			retryToken = this.ResetDebounceTokenLocked();
		}
		this.LaunchDebouncedReconcile(retryDelay, retryToken);
	}

	/// <summary>
	/// 未処理変更リストを、今回のトリガーで再評価すべきスコープ内アイテムと維持するスコープ外アイテムに分割します。
	/// </summary>
	/// <param name="dirtyPaths">トリガーとなったダーティパス集合</param>
	/// <param name="renameHistory">時系列のリネーム履歴</param>
	/// <param name="scopedExistingItems">再評価対象となる既存アイテム一覧</param>
	/// <param name="untouchedExistingItems">維持される既存アイテム一覧</param>
	private void SplitExistingItemsLocked(
		HashSet<string> dirtyPaths,
		List<RenameOperation> renameHistory,
		out List<FileChangeItem> scopedExistingItems,
		out List<FileChangeItem> untouchedExistingItems) {
		var triggerPaths = new HashSet<string>(dirtyPaths, StringComparer.OrdinalIgnoreCase);
		foreach (var op in renameHistory) {
			triggerPaths.Add(op.OldPath);
			triggerPaths.Add(op.NewPath);
		}

		scopedExistingItems = [];
		untouchedExistingItems = [];
		foreach (var existing in this.UnprocessedChanges) {
			if (IsExistingItemInScope(existing, triggerPaths)) {
				scopedExistingItems.Add(existing);
			} else {
				untouchedExistingItems.Add(existing);
			}
		}
	}

	/// <summary>
	/// 指定パスの読み取り失敗に対するリトライ回数を消費し、再試行を継続可能かを返します。
	/// </summary>
	/// <param name="path">対象ファイルパス</param>
	/// <returns>リトライ上限以内で再試行できる場合は true</returns>
	private bool TryConsumeRetrySlot(string path) {
		lock (this._lock) {
			var count = this._retryCounts.GetValueOrDefault(path, 0) + 1;
			if (count <= MaxFileReadRetryCount) {
				this._retryCounts[path] = count;
				return true;
			}
			this._retryCounts.Remove(path);
			return false;
		}
	}

	/// <summary>
	/// 指定パスの読み取りリトライ回数カウントをリセットします。
	/// </summary>
	/// <param name="path">対象ファイルパス</param>
	private void ResetRetryCount(string path) {
		lock (this._lock) {
			this._retryCounts.Remove(path);
		}
	}

	/// <summary>
	/// DB世代の競合および実行中の除外操作を考慮し、差分計算結果を <see cref="UnprocessedChanges"/> へ反映します。
	/// </summary>
	/// <param name="scopedExistingItems">今回の評価対象とした既存アイテム一覧</param>
	/// <param name="reconciledItems">新しく確定した変更アイテム一覧</param>
	/// <param name="upgradedUntouchedItems">Moved へ昇格した既存アイテムの置換ペア一覧</param>
	/// <param name="reconcileGeneration">今回の差分計算世代</param>
	/// <param name="startDbGeneration">差分計算開始時点の DB 更新世代</param>
	/// <returns>コミットに成功した（または破棄済みで終了する）場合は true、DB更新競合で再計算が必要な場合は false</returns>
	private bool TryCommitReconciledItems(
		List<FileChangeItem> scopedExistingItems,
		List<FileChangeItem> reconciledItems,
		List<(FileChangeItem OldItem, FileChangeItem NewItem)> upgradedUntouchedItems,
		long reconcileGeneration,
		long startDbGeneration) {
		lock (this._lock) {
			if (this._isDisposed) {
				return true;
			}

			if (this._dbMutationGeneration != startDbGeneration) {
				return false;
			}

			bool IsRemovedInGeneration(FileChangeItem item) {
				return this._removedDuringReconcile.Any(r => r.Generation == reconcileGeneration && IsSameChangeItem(r.Item, item));
			}

			var filteredReconciled = reconciledItems
				.Where(item => !IsRemovedInGeneration(item))
				.ToList();

			var toRemove = new List<FileChangeItem>();
			var toAdd = new List<FileChangeItem>();
			var matchedReconciled = new HashSet<FileChangeItem>();

			foreach (var existing in scopedExistingItems) {
				var equivalent = filteredReconciled.FirstOrDefault(r => !matchedReconciled.Contains(r) && AreEquivalent(existing, r));
				if (equivalent != null) {
					matchedReconciled.Add(equivalent);
				} else {
					toRemove.Add(existing);
				}
			}

			foreach (var item in filteredReconciled) {
				if (!matchedReconciled.Contains(item)) {
					toAdd.Add(item);
				}
			}

			foreach (var (oldItem, newItem) in upgradedUntouchedItems) {
				if (IsRemovedInGeneration(oldItem) || IsRemovedInGeneration(newItem)) {
					continue;
				}
				toRemove.Add(oldItem);
				toAdd.Add(newItem);
			}

			if (toRemove.Count > 0) {
				this.UnprocessedChanges.RemoveRange(toRemove);
			}
			if (toAdd.Count > 0) {
				this.UnprocessedChanges.AddRange(toAdd);
			}

			return true;
		}
	}

	/// <summary>
	/// 既存の未処理アイテムが今回のトリガーパス集合の影響範囲に含まれるかを判定します。
	/// </summary>
	/// <param name="existing">判定対象の既存アイテム</param>
	/// <param name="triggerPaths">トリガーとなったパス集合</param>
	/// <returns>影響範囲に含まれる場合は true</returns>
	private static bool IsExistingItemInScope(FileChangeItem existing, HashSet<string> triggerPaths) {
		var oldNorm = FileChangePathUtility.NormalizePath(existing.OldPath);
		var newNorm = FileChangePathUtility.NormalizePath(existing.NewPath);
		foreach (var trigger in triggerPaths) {
			if (!string.IsNullOrEmpty(oldNorm) && FileChangePathUtility.IsSelfOrDescendant(oldNorm, trigger)) {
				return true;
			}
			if (!string.IsNullOrEmpty(newNorm) && FileChangePathUtility.IsSelfOrDescendant(newNorm, trigger)) {
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// 2つの <see cref="FileChangeItem"/> が同一の変更操作（ID・変更種別・旧新パス）を表すかを判定します。
	/// </summary>
	/// <param name="a">比較対象 A</param>
	/// <param name="b">比較対象 B</param>
	/// <returns>同一の変更操作である場合は true</returns>
	private static bool IsSameChangeItem(FileChangeItem a, FileChangeItem b) {
		if (ReferenceEquals(a, b)) {
			return true;
		}
		return a.MediaItemId == b.MediaItemId
			&& a.ChangeType == b.ChangeType
			&& string.Equals(FileChangePathUtility.NormalizePath(a.OldPath), FileChangePathUtility.NormalizePath(b.OldPath), StringComparison.Ordinal)
			&& string.Equals(FileChangePathUtility.NormalizePath(a.NewPath), FileChangePathUtility.NormalizePath(b.NewPath), StringComparison.Ordinal);
	}

	/// <summary>
	/// 2つの <see cref="FileChangeItem"/> のすべての主要フィールドが等価かを判定します。
	/// </summary>
	/// <param name="a">比較対象 A</param>
	/// <param name="b">比較対象 B</param>
	/// <returns>内容が完全に等価である場合は true</returns>
	private static bool AreEquivalent(FileChangeItem a, FileChangeItem b) {
		return a.MediaItemId == b.MediaItemId
			&& a.ChangeType == b.ChangeType
			&& string.Equals(a.OldPath, b.OldPath, StringComparison.Ordinal)
			&& string.Equals(a.NewPath, b.NewPath, StringComparison.Ordinal)
			&& a.FileSize == b.FileSize
			&& string.Equals(a.OldHash, b.OldHash, StringComparison.Ordinal)
			&& string.Equals(a.NewHash, b.NewHash, StringComparison.Ordinal);
	}

	/// <summary>
	/// リソースを破棄します。
	/// </summary>
	/// <param name="disposing">マネージドリソースを破棄するかどうか</param>
	protected override void Dispose(bool disposing) {
		if (disposing) {
			var shouldDispose = false;
			lock (this._lock) {
				if (!this._isDisposed) {
					this._isDisposed = true;
					this._debounceCts?.Cancel();
					this._debounceCts?.Dispose();
					this._debounceCts = null;
					shouldDispose = true;
				}
			}
			if (shouldDispose) {
				this._disposeCts.Cancel();
				this._disposeCts.Dispose();
				this._processLock.Dispose();
			}
		}
		base.Dispose(disposing);
	}

	/// <summary>
	/// 1回の差分計算サイクルで扱う入力スナップショットおよび世代情報を保持するバッチクラスです。
	/// </summary>
	private sealed class ReconcileBatch {
		/// <summary>
		/// <see cref="ReconcileBatch"/> クラスの新しいインスタンスを初期化します。
		/// </summary>
		/// <param name="dirtyPaths">変更トリガーとなったパス集合</param>
		/// <param name="renameHistory">時系列のリネーム履歴</param>
		/// <param name="scopedExistingItems">再評価対象となる既存アイテム一覧</param>
		/// <param name="untouchedExistingItems">維持する既存アイテム一覧</param>
		/// <param name="reconcileGeneration">差分計算世代</param>
		/// <param name="startDbGeneration">開始時の DB 更新世代</param>
		public ReconcileBatch(
			HashSet<string> dirtyPaths,
			List<RenameOperation> renameHistory,
			List<FileChangeItem> scopedExistingItems,
			List<FileChangeItem> untouchedExistingItems,
			long reconcileGeneration,
			long startDbGeneration) {
			this.DirtyPaths = dirtyPaths;
			this.RenameHistory = renameHistory;
			this.ScopedExistingItems = scopedExistingItems;
			this.UntouchedExistingItems = untouchedExistingItems;
			this.ReconcileGeneration = reconcileGeneration;
			this.StartDbGeneration = startDbGeneration;
		}

		/// <summary>
		/// 変更トリガーとなったパス集合
		/// </summary>
		public HashSet<string> DirtyPaths {
			get;
		}

		/// <summary>
		/// 時系列のリネーム履歴
		/// </summary>
		public List<RenameOperation> RenameHistory {
			get;
		}

		/// <summary>
		/// 再評価対象となる既存アイテム一覧
		/// </summary>
		public List<FileChangeItem> ScopedExistingItems {
			get;
			set;
		}

		/// <summary>
		/// 評価対象外として維持する既存アイテム一覧
		/// </summary>
		public List<FileChangeItem> UntouchedExistingItems {
			get;
			set;
		}

		/// <summary>
		/// このバッチの差分計算世代
		/// </summary>
		public long ReconcileGeneration {
			get;
		}

		/// <summary>
		/// 評価開始時点の DB 更新世代
		/// </summary>
		public long StartDbGeneration {
			get;
			set;
		}
	}
}