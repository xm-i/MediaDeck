using System.Threading.Tasks;

using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.FileTypes.Models;
using MediaDeck.Database;
using MediaDeck.Database.Tables;

namespace MediaDeck.FileTypes.Base.Models;

internal abstract class BaseFileOperator : IFileOperator {
	protected readonly IDbContextFactory<MediaDeckDbContext> _dbFactory;
	protected readonly IFileHashUpdatorService _updateFileHashBackgroundService;

	protected BaseFileOperator(
		IDbContextFactory<MediaDeckDbContext> dbFactory,
		IFileHashUpdatorService updateFileHashBackgroundService,
		MediaType targetMediaType) {
		this._dbFactory = dbFactory;
		this._updateFileHashBackgroundService = updateFileHashBackgroundService;
		this.TargetMediaType = targetMediaType;
	}

	public virtual async Task<MediaFile?> UpdateRateAsync(long mediaFileId, int rate) {
		await using var db = await this._dbFactory.CreateDbContextAsync();
		using var transaction = await db.Database.BeginTransactionAsync();
		var file = await db.MediaFiles.FirstOrDefaultAsync(x => x.MediaFileId == mediaFileId);
		if (file is not { } mediaFile) {
			return null;
		}
		mediaFile.Rate = rate;
		db.Update(mediaFile);
		await db.SaveChangesAsync();
		await transaction.CommitAsync();
		return mediaFile;
	}

	public virtual async Task<MediaFile?> IncrementUsageCountAsync(long mediaFileId) {
		await using var db = await this._dbFactory.CreateDbContextAsync();
		using var transaction = await db.Database.BeginTransactionAsync();
		var file = await db.MediaFiles.FirstOrDefaultAsync(x => x.MediaFileId == mediaFileId);
		if (file is not { } mediaFile) {
			return null;
		}
		mediaFile.UsageCount++;
		db.Update(mediaFile);
		await db.SaveChangesAsync();
		await transaction.CommitAsync();
		return mediaFile;
	}

	public virtual async Task<MediaFile?> UpdateDescriptionAsync(long mediaFileId, string description) {
		await using var db = await this._dbFactory.CreateDbContextAsync();
		using var transaction = await db.Database.BeginTransactionAsync();
		var file = await db.MediaFiles.FirstOrDefaultAsync(x => x.MediaFileId == mediaFileId);
		if (file is not { } mediaFile) {
			return null;
		}
		mediaFile.Description = description;
		db.Update(mediaFile);
		await db.SaveChangesAsync();
		await transaction.CommitAsync();
		return mediaFile;
	}


	public MediaType TargetMediaType {
		get;
	}

	public abstract Task<MediaFile?> RegisterFileAsync(string filePath);
}