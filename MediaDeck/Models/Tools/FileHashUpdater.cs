using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

using MediaDeck.Database;
using MediaDeck.Utils.Constants;
using MediaDeck.Utils.Tools;

using Microsoft.EntityFrameworkCore;

namespace MediaDeck.Models.Tools;

[Inject(InjectServiceLifetime.Singleton)]
public class FileHashUpdater {
	private readonly MediaDeckDbContext _db;

	public ObservableQueue<long> HashUpdateQueue {
		get;
	} = [];

	public ReactiveProperty<long> TargetCount {
		get;
	} = new();

	public ReactiveProperty<long> CompletedCount {
		get;
	} = new();

	public ObservableQueue<long> FullHashUpdateQueue {
		get;
	} = [];

	public ReactiveProperty<long> FullHashTargetCount {
		get;
	} = new();

	public ReactiveProperty<long> FullHashCompletedCount {
		get;
	} = new();

	public FileHashUpdater(MediaDeckDbContext db) {
		this._db = db;
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

	public void EnqueueHashUpdate(long mediaFileId) {
		this.HashUpdateQueue.Enqueue(mediaFileId);
		this.TargetCount.Value++;
	}

	public void EnqueueHashUpdateRange(IEnumerable<long> mediaFileIds) {
		var ids = mediaFileIds.ToList();
		this.HashUpdateQueue.EnqueueRange(ids);
		this.TargetCount.Value += ids.Count;
	}

	public async Task CheckAndEnqueueFullHashUpdatesAsync() {
		// PreHash更新がない場合でも、重複PreHashのFullHashチェックを行う
		if (this.HashUpdateQueue.Count == 0) {
			await this.EnqueueDuplicatePreHashForFullHashAsync();
		}
	}

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

				using (await LockObjectConstants.DbLock.LockAsync()) {
					var mediaFile = await this._db.MediaFiles.FindAsync(mediaFileId);
					if (mediaFile != null) {
						mediaFile.PreHash = hash;
						mediaFile.PreHashUpdatedTime = DateTime.Now;
						await this._db.SaveChangesAsync();
					}
				}
			} catch (Exception e) {
				Console.WriteLine(e);
			} finally {
				this.CompletedCount.Value++;
			}
		}

		// PreHashキューが空になったら、重複PreHashを持つレコードのFullHashを生成
		await this.EnqueueDuplicatePreHashForFullHashAsync();
	}

	private async Task EnqueueDuplicatePreHashForFullHashAsync() {
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

				using (await LockObjectConstants.DbLock.LockAsync()) {
					var mediaFile = await this._db.MediaFiles.FindAsync(mediaFileId);
					if (mediaFile != null) {
						mediaFile.FullHash = fullHash;
						mediaFile.FullHashUpdatedTime = DateTime.Now;
						await this._db.SaveChangesAsync();
					}
				}
			} catch (Exception e) {
				Console.WriteLine(e);
			} finally {
				this.FullHashCompletedCount.Value++;
			}
		}
	}
}
