using System.IO;
using System.Threading.Tasks;
using MediaDeck.Composition.Database;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;
using MediaDeck.Composition.Tables;

namespace MediaDeck.MediaItemTypes.Base.Models;

public abstract class BaseMediaItemOperator : IMediaItemOperator {
	protected readonly IDbContextFactory<MediaDeckDbContext> _dbFactory;
	protected readonly IFileHashUpdatorService _updateFileHashBackgroundService;

	protected BaseMediaItemOperator(
		IDbContextFactory<MediaDeckDbContext> dbFactory,
		IFileHashUpdatorService updateFileHashBackgroundService,
		MediaType targetMediaType) {
		this._dbFactory = dbFactory;
		this._updateFileHashBackgroundService = updateFileHashBackgroundService;
		this.TargetMediaType = targetMediaType;
	}

	public virtual async Task<MediaItem?> UpdateRateAsync(long MediaItemId, int rate) {
		await using var db = await this._dbFactory.CreateDbContextAsync();
		using var transaction = await db.Database.BeginTransactionAsync();
		var file = await db.MediaItems.FirstOrDefaultAsync(x => x.MediaItemId == MediaItemId);
		if (file is not { } MediaItem) {
			return null;
		}
		MediaItem.Rate = rate;
		db.Update(MediaItem);
		await db.SaveChangesAsync();
		await transaction.CommitAsync();
		return MediaItem;
	}

	public virtual async Task<MediaItem?> IncrementUsageCountAsync(long MediaItemId) {
		await using var db = await this._dbFactory.CreateDbContextAsync();
		using var transaction = await db.Database.BeginTransactionAsync();
		var file = await db.MediaItems.FirstOrDefaultAsync(x => x.MediaItemId == MediaItemId);
		if (file is not { } MediaItem) {
			return null;
		}
		MediaItem.UsageCount++;
		db.Update(MediaItem);
		await db.SaveChangesAsync();
		await transaction.CommitAsync();
		return MediaItem;
	}

	public virtual async Task<MediaItem?> UpdateDescriptionAsync(long MediaItemId, string description) {
		await using var db = await this._dbFactory.CreateDbContextAsync();
		using var transaction = await db.Database.BeginTransactionAsync();
		var file = await db.MediaItems.FirstOrDefaultAsync(x => x.MediaItemId == MediaItemId);
		if (file is not { } MediaItem) {
			return null;
		}
		MediaItem.Description = description;
		db.Update(MediaItem);
		await db.SaveChangesAsync();
		await transaction.CommitAsync();
		return MediaItem;
	}


	public MediaType TargetMediaType {
		get;
	}

	protected async Task<bool> GetIsUnderFolderGroup(MediaDeckDbContext dbContext, string directoryPath) {
		return await dbContext.MediaItems.AnyAsync(x => directoryPath == x.FilePath && directoryPath.StartsWith(x.FilePath + Path.PathSeparator));
	}

	public abstract Task<MediaItem?> RegisterMediaItemAsync(string filePath);
}