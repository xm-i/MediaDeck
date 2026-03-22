using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using MediaDeck.Database;
using MediaDeck.Database.Tables;

namespace MediaDeck.Models.Tools;
[Inject(InjectServiceLifetime.Transient)]
public class FileStatusUpdater {
	public FileStatusUpdater(IDbContextFactory<MediaDeckDbContext> dbFactory, FileHashUpdater fileHashUpdater) {
		this._dbFactory = dbFactory;
		this._fileHashUpdater = fileHashUpdater;
	}
	private readonly IDbContextFactory<MediaDeckDbContext> _dbFactory;
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
		await using (var db = await this._dbFactory.CreateDbContextAsync()) {
			targetFiles = await db.MediaFiles.ToListAsync();
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
						file.PreHashUpdatedTime != null &&
						file.PreHashUpdatedTime >= fileInfo.LastWriteTime
						)
					)
				) {
				continue;
			}
			var needsHashUpdate = fileInfo.Exists && (file.PreHashUpdatedTime == null || file.PreHashUpdatedTime < fileInfo.LastWriteTime);

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

		await using (var db = await this._dbFactory.CreateDbContextAsync()) {
			using var transaction = await db.Database.BeginTransactionAsync();
			db.UpdateRange(updateList);
			await db.SaveChangesAsync();
			await transaction.CommitAsync();
		}

		// PreHash更新がなかった場合もFullHashのチェックを行う
		await this._fileHashUpdater.CheckAndEnqueueFullHashUpdatesAsync();
	}
}
