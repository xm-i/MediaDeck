using MediaDeck.Database;
using MediaDeck.Database.Tables;

namespace MediaDeck.Core.Services.FileStatusUpdator;

[Inject(InjectServiceLifetime.Transient)]
public class FileStatusUpdatorService {
	public FileStatusUpdatorService(IDbContextFactory<MediaDeckDbContext> dbFactory, IFileHashUpdatorService fileHashUpdatorService) {
		this._dbFactory = dbFactory;
		this._fileHashUpdatorService = fileHashUpdatorService;
	}

	private readonly IDbContextFactory<MediaDeckDbContext> _dbFactory;
	private readonly IFileHashUpdatorService _fileHashUpdatorService;

	public ReactiveProperty<long> TargetCount {
		get;
	} = new();

	public ReactiveProperty<long> CompletedCount {
		get;
	} = new();

	public async Task UpdateFileInfo() {
		var updateList = new List<MediaItem>();
		var targetFiles = new List<MediaItem>();
		await using (var db = await this._dbFactory.CreateDbContextAsync()) {
			targetFiles = await db.MediaItems.ToListAsync();
		}
		this.TargetCount.Value = targetFiles.Count;
		this.CompletedCount.Value = 0;
		foreach (var file in targetFiles) {
			this.CompletedCount.Value++;
			var fileInfo = new FileInfo(file.FilePath);
			if (fileInfo == null) {
				continue;
			}
			if (
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
					this._fileHashUpdatorService.EnqueueHashUpdate(file.MediaItemId);
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
		await this._fileHashUpdatorService.CheckAndEnqueueFullHashUpdatesAsync();
	}
}