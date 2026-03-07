using System.Threading.Tasks;
using System.Collections.Generic;

using MediaDeck.Database;
using MediaDeck.Utils.Constants;

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
						mediaFile.Hash = hash;
						mediaFile.HashUpdatedTime = DateTime.Now;
						await this._db.SaveChangesAsync();
					}
				}
			} catch (Exception e) {
				Console.WriteLine(e);
			} finally {
				this.CompletedCount.Value++;
			}
		}
	}
}
