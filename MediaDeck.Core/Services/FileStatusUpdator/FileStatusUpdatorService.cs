using MediaDeck.Composition.Database;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Tables;

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

	public async Task UpdateFileInfo(CancellationToken ct = default) {
		var updateList = new List<MediaItem>();
		var targetFiles = new List<MediaItem>();
		await using (var db = await this._dbFactory.CreateDbContextAsync(ct)) {
			targetFiles = await db.MediaItems.ToListAsync(ct);
		}
		this.TargetCount.Value = targetFiles.Count;
		this.CompletedCount.Value = 0;
		foreach (var file in targetFiles) {
			if (ct.IsCancellationRequested) {
				return;
			}
			this.CompletedCount.Value++;
			var mediaItemTypeProvider = this._mediaItemTypeService.GetMediaItemTypeProvider(file.MediaType);
			var pathStatus = mediaItemTypeProvider.GetPathStatus(file.FilePath);
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
			var needsHashUpdate = pathStatus.Exists && (file.PreHashUpdatedTime == null || file.PreHashUpdatedTime < pathStatus.ModifiedTime) && file.MediaType != MediaType.FolderGroup;

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

		if (updateList.Any()) {
			await using (var db = await this._dbFactory.CreateDbContextAsync(ct)) {
				using var transaction = await db.Database.BeginTransactionAsync(ct);
				db.UpdateRange(updateList);
				await db.SaveChangesAsync(ct);
				await transaction.CommitAsync(ct);
			}
		}

		// PreHash更新がなかった場合もFullHashのチェックを行う
		await this._fileHashUpdatorService.CheckAndEnqueueFullHashUpdatesAsync(ct);
	}
}