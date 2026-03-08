using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

using MediaDeck.Database;
using MediaDeck.Utils.Constants;
using MediaDeck.Utils.Tools;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MediaDeck.Models.Tools;

/// <summary>
/// メディアファイルのハッシュ値（PreHashおよびFullHash）を管理し、更新するクラス。
/// PreHashは高速な部分ハッシュで、FullHashは完全なファイルハッシュ。
/// PreHashが重複する場合にのみFullHashを計算し、重複がなくなった場合はFullHashをクリアする。
/// </summary>
[Inject(InjectServiceLifetime.Singleton)]
public class FileHashUpdater {
	private readonly MediaDeckDbContext _db;
	private readonly ILogger<FileHashUpdater> _logger;

	/// <summary>
	/// PreHash更新待ちのメディアファイルIDを保持するキュー
	/// </summary>
	public ObservableQueue<long> HashUpdateQueue {
		get;
	} = [];

	/// <summary>
	/// PreHash更新の対象となるファイルの総数
	/// </summary>
	public ReactiveProperty<long> TargetCount {
		get;
	} = new();

	/// <summary>
	/// PreHash更新が完了したファイルの数
	/// </summary>
	public ReactiveProperty<long> CompletedCount {
		get;
	} = new();

	/// <summary>
	/// FullHash更新待ちのメディアファイルIDを保持するキュー
	/// </summary>
	public ObservableQueue<long> FullHashUpdateQueue {
		get;
	} = [];

	/// <summary>
	/// FullHash更新の対象となるファイルの総数
	/// </summary>
	public ReactiveProperty<long> FullHashTargetCount {
		get;
	} = new();

	/// <summary>
	/// FullHash更新が完了したファイルの数
	/// </summary>
	public ReactiveProperty<long> FullHashCompletedCount {
		get;
	} = new();

	/// <summary>
	/// FileHashUpdaterクラスの新しいインスタンスを初期化する。
	/// キューの監視とハッシュ更新処理のサブスクリプションを設定する。
	/// </summary>
	/// <param name="db">データベースコンテキスト</param>
	/// <param name="logger">ロガー</param>
	public FileHashUpdater(MediaDeckDbContext db, ILogger<FileHashUpdater> logger) {
		this._db = db;
		this._logger = logger;
		this.HashUpdateQueue
			.ObserveAdd()
			.ThrottleFirst(TimeSpan.FromSeconds(0.1))
			.ObserveOnThreadPool()
			.SubscribeAwait(
				async (x, ct) =>
					await this.UpdateHashAsync().ConfigureAwait(false),
					AwaitOperation.Sequential,
					false);

		this.FullHashUpdateQueue
			.ObserveAdd()
			.ThrottleFirst(TimeSpan.FromSeconds(0.1))
			.ObserveOnThreadPool()
			.SubscribeAwait(
				async (x, ct) =>
					await this.UpdateFullHashAsync().ConfigureAwait(false),
					AwaitOperation.Sequential,
					false);
	}

	/// <summary>
	/// 指定されたメディアファイルのPreHash更新をキューに追加する
	/// </summary>
	/// <param name="mediaFileId">メディアファイルID</param>
	public void EnqueueHashUpdate(long mediaFileId) {
		this.HashUpdateQueue.Enqueue(mediaFileId);
		this.TargetCount.Value++;
	}

	/// <summary>
	/// 複数のメディアファイルのPreHash更新をキューに一括追加する
	/// </summary>
	/// <param name="mediaFileIds">メディアファイルIDのコレクション</param>
	public void EnqueueHashUpdateRange(IEnumerable<long> mediaFileIds) {
		var ids = mediaFileIds.ToList();
		this.HashUpdateQueue.EnqueueRange(ids);
		this.TargetCount.Value += ids.Count;
	}

	/// <summary>
	/// PreHash更新キューが空の場合に、重複PreHashのチェックとFullHash管理を実行する
	/// </summary>
	public async Task CheckAndEnqueueFullHashUpdatesAsync() {
		// PreHash更新がない場合でも、重複PreHashのFullHashチェックを行う
		if (this.HashUpdateQueue.Count == 0) {
			await this.EnqueueDuplicatePreHashForFullHashAsync();
		}
	}

	/// <summary>
	/// キューに追加されたメディアファイルのPreHashを順次更新する。
	/// 全ての更新完了後、重複PreHashのチェックとFullHash管理を実行する。
	/// </summary>
	private async Task UpdateHashAsync() {
		while (this.HashUpdateQueue.TryDequeue(out var mediaFileId)) {
			try {
				string? filePath;
				using (await LockObjectConstants.DbLock.LockAsync()) {
					var mediaFile = await this._db.MediaFiles.FindAsync(mediaFileId);
					if (mediaFile == null || !mediaFile.IsExists) {
						continue;
					}
					filePath = mediaFile.FilePath;
				}

				var hash = FileHashUtility.ComputeFileHash(filePath);

				using (await LockObjectConstants.DbLock.LockAsync())
				using (var transaction = await this._db.Database.BeginTransactionAsync()) {
					var mediaFile = await this._db.MediaFiles.FindAsync(mediaFileId);
					if (mediaFile != null) {
						mediaFile.PreHash = hash;
						mediaFile.PreHashUpdatedTime = DateTime.Now;
						await this._db.SaveChangesAsync();
						await transaction.CommitAsync();
					}
				}
			} catch (Exception e) {
				this._logger.LogError(e, "Error while updating PreHash for MediaFileId {MediaFileId}", mediaFileId);
			} finally {
				this.CompletedCount.Value++;
			}
		}

		// PreHashキューが空になったら、重複PreHashを持つレコードのFullHashを生成し、重複がなくなったレコードのFullHashをクリア。
		await this.ClearFullHashForNonDuplicatePreHashAsync();
		await this.EnqueueDuplicatePreHashForFullHashAsync();
	}

	/// <summary>
	/// PreHashの重複がある場合はFullHashを更新する。
	/// </summary>
	private async Task EnqueueDuplicatePreHashForFullHashAsync() {
		await this.EnqueueFullHashUpdatesForDuplicatePreHashAsync();
	}

	/// <summary>
	/// PreHashが重複しているメディアファイルのFullHash更新をキューに追加する。
	/// FullHashが未設定、またはPreHashより古い場合に更新対象とする。
	/// </summary>
	private async Task EnqueueFullHashUpdatesForDuplicatePreHashAsync() {
		List<long> duplicateIds;
		using (await LockObjectConstants.DbLock.LockAsync()) {
			// PreHashが同一のレコードが2つ以上あるグループを見つけ、
			// その中でFullHashが未設定またはPreHashより古いものを抽出
			var duplicatePreHashes = await this._db.MediaFiles
				.Where(m => m.IsExists && m.PreHash != null)
				.GroupBy(m => m.PreHash)
				.Where(g => g.Count() >= 2)
				.Select(g => g.Key)
				.ToListAsync();

			duplicateIds = await this._db.MediaFiles
				.Where(m => m.IsExists &&
					duplicatePreHashes.Contains(m.PreHash) &&
					(m.FullHash == null || m.PreHashUpdatedTime > m.FullHashUpdatedTime))
				.Select(m => m.MediaFileId)
				.ToListAsync();
		}

		if (duplicateIds.Count > 0) {
			this.FullHashUpdateQueue.EnqueueRange(duplicateIds);
			this.FullHashTargetCount.Value += duplicateIds.Count;
		}
	}

	/// <summary>
	/// PreHashが重複していないメディアファイルのFullHashとFullHashUpdatedTimeをクリアする。
	/// 重複が解消されたファイルから不要なFullHashを削除する。
	/// </summary>
	private async Task ClearFullHashForNonDuplicatePreHashAsync() {
		List<long> nonDuplicateIdsWithFullHash;
		using var lockObject = await LockObjectConstants.DbLock.LockAsync();
		using var transaction = await this._db.Database.BeginTransactionAsync();
			// PreHashが重複しているグループを特定
			var duplicatePreHashes = await this._db.MediaFiles
			.Where(m => m.IsExists && m.PreHash != null)
			.GroupBy(m => m.PreHash)
			.Where(g => g.Count() >= 2)
			.Select(g => g.Key)
			.ToListAsync();

		// PreHashが重複していないのにFullHashが設定されているレコードを見つける
		nonDuplicateIdsWithFullHash = await this._db.MediaFiles
			.Where(m => m.IsExists &&
				m.PreHash != null &&
				!duplicatePreHashes.Contains(m.PreHash) &&
				m.FullHash != null)
			.Select(m => m.MediaFileId)
			.ToListAsync();

		// PreHashが重複していないレコードのFullHashをクリア
		if (nonDuplicateIdsWithFullHash.Count > 0) {
			foreach (var id in nonDuplicateIdsWithFullHash) {
				var mediaFile = await this._db.MediaFiles.FindAsync(id);
				if (mediaFile != null) {
					mediaFile.FullHash = null;
					mediaFile.FullHashUpdatedTime = null;
				}
			}
			await this._db.SaveChangesAsync();
			await transaction.CommitAsync();
		}
	}

	/// <summary>
	/// キューに追加されたメディアファイルのFullHashを順次更新する。
	/// ファイル全体をスキャンして完全なハッシュ値を計算し、データベースに保存する。
	/// </summary>
	private async Task UpdateFullHashAsync() {
		while (this.FullHashUpdateQueue.TryDequeue(out var mediaFileId)) {
			try {
				string? filePath;
				using (await LockObjectConstants.DbLock.LockAsync()) {
					var mediaFile = await this._db.MediaFiles.FindAsync(mediaFileId);
					if (mediaFile == null || !mediaFile.IsExists) {
						continue;
					}
					filePath = mediaFile.FilePath;
				}

				var fullHash = FileHashUtility.ComputeFullFileHash(filePath);

				using (await LockObjectConstants.DbLock.LockAsync())
				using (var transaction = await this._db.Database.BeginTransactionAsync()) {
					var mediaFile = await this._db.MediaFiles.FindAsync(mediaFileId);
					if (mediaFile != null) {
						mediaFile.FullHash = fullHash;
						mediaFile.FullHashUpdatedTime = DateTime.Now;
						await this._db.SaveChangesAsync();
						await transaction.CommitAsync();
					}
				}
			} catch (Exception e) {
				this._logger.LogError(e, "Error while updating FullHash for MediaFileId {MediaFileId}", mediaFileId);
			} finally {
				this.FullHashCompletedCount.Value++;
			}
		}
	}
}
