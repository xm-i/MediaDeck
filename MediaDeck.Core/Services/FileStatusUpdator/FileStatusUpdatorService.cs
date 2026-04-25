using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Database;
using MediaDeck.Database.Tables;

namespace MediaDeck.Core.Services.FileStatusUpdator;

[Inject(InjectServiceLifetime.Transient)]
public class FileStatusUpdatorService {
	public FileStatusUpdatorService(IDbContextFactory<MediaDeckDbContext> dbFactory, IFileHashUpdatorService fileHashUpdatorService, IMediaItemTypeService mediaItemTypeService) {
		this._dbFactory = dbFactory;
		this._fileHashUpdatorService = fileHashUpdatorService;
		this._mediaItemTypeService = mediaItemTypeService;
	}

	private readonly IDbContextFactory<MediaDeckDbContext> _dbFactory;
	private readonly IFileHashUpdatorService _fileHashUpdatorService;
	private readonly IMediaItemTypeService _mediaItemTypeService;

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
			var mediaItemType = this._mediaItemTypeService.GetMediaItemType(file);
			var pathStatus = mediaItemType.GetPathStatus(file.FilePath);
			if (
				file.IsExists == pathStatus.Exists &&
				(!file.IsExists ||
					(
						file.FileSize == pathStatus.FileSize &&
						file.CreationTime == pathStatus.CreationTime &&
						file.ModifiedTime == pathStatus.ModifiedTime &&
						file.LastAccessTime == pathStatus.LastAccessTime &&
						file.PreHashUpdatedTime != null &&
						file.PreHashUpdatedTime >= pathStatus.ModifiedTime
					)
				)
			) {
				continue;
			}
			var needsHashUpdate = pathStatus.Exists && (file.PreHashUpdatedTime == null || file.PreHashUpdatedTime < pathStatus.ModifiedTime);

			file.IsExists = pathStatus.Exists;

			if (file.IsExists) {
				if (needsHashUpdate) {
					this._fileHashUpdatorService.EnqueueHashUpdate(file.MediaItemId);
				}
				file.FileSize = pathStatus.FileSize;
				file.CreationTime = pathStatus.CreationTime;
				file.ModifiedTime = pathStatus.ModifiedTime;
				file.LastAccessTime = pathStatus.LastAccessTime;
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