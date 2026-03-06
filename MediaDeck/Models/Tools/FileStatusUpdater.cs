using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using MediaDeck.Database;
using MediaDeck.Database.Tables;
using MediaDeck.Utils.Constants;

namespace MediaDeck.Models.Tools;
[Inject(InjectServiceLifetime.Transient)]
public class FileStatusUpdater {
	public FileStatusUpdater(MediaDeckDbContext db, FileHashUpdater fileHashUpdater) {
		this._db = db;
		this._fileHashUpdater = fileHashUpdater;
	}
	private readonly MediaDeckDbContext _db;
	private readonly FileHashUpdater _fileHashUpdater;

	public ReactiveProperty<long> TargetCount {
		get;
	} = new();

	public ReactiveProperty<long> CompletedCount {
		get;
	} = new();

	public async Task UpdateFileInfo() {
		var updateList = new List<MediaFile>();
		var targetFiles = new List<MediaFile>();
		using (await LockObjectConstants.DbLock.LockAsync()) {
			targetFiles = await this._db.MediaFiles.ToListAsync();
		}
		this.TargetCount.Value = targetFiles.Count;
		this.CompletedCount.Value = 0;
		foreach (var file in targetFiles) {
			this.CompletedCount.Value++;
			var fileInfo = new FileInfo(file.FilePath);
			if (fileInfo == null) {
				continue;
			}
			if(
					file.IsExists == fileInfo.Exists &&
						(!file.IsExists ||
							(
							file.FileSize == fileInfo.Length &&
							file.CreationTime == fileInfo.CreationTime &&
							file.ModifiedTime == fileInfo.LastWriteTime &&
							file.LastAccessTime == fileInfo.LastAccessTime &&
							file.HashUpdatedTime >= fileInfo.LastWriteTime &&
							file.HashUpdatedTime != null
							)
						)
					) {
					continue;
				}
				var needsHashUpdate = fileInfo.Exists && (file.HashUpdatedTime == null || file.HashUpdatedTime < fileInfo.LastWriteTime);

				file.IsExists = fileInfo.Exists;

				if (file.IsExists) {
					if (needsHashUpdate) {
						this._fileHashUpdater.EnqueueHashUpdate(file.MediaFileId);
					}
					file.FileSize = fileInfo.Length;
					file.CreationTime = fileInfo.CreationTime;
					file.ModifiedTime = fileInfo.LastWriteTime;
					file.LastAccessTime = fileInfo.LastAccessTime;
				}
				updateList.Add(file);
			}

		using var lockObject = await LockObjectConstants.DbLock.LockAsync();
		using var transaction = await this._db.Database.BeginTransactionAsync();
		this._db.UpdateRange(updateList);
		await this._db.SaveChangesAsync();
		await transaction.CommitAsync();
	}
}
