using System.Threading.Tasks;

using CommunityToolkit.Mvvm.DependencyInjection;

using MediaDeck.Composition.Enum;
using MediaDeck.Database;
using MediaDeck.Database.Tables;
using MediaDeck.FileTypes.Base.Models.Interfaces;
using MediaDeck.Utils.Constants;

namespace MediaDeck.FileTypes.Base.Models;

public abstract class BaseFileOperator : IFileOperator {
	protected readonly MediaDeckDbContext _db;

	protected BaseFileOperator() {
		this._db = Ioc.Default.GetRequiredService<MediaDeckDbContext>();
	}

	public virtual async Task<MediaFile?> UpdateRateAsync(long mediaFileId, int rate) {
		using var lockObject = await LockObjectConstants.DbLock.LockAsync();
		using var transaction = await this._db.Database.BeginTransactionAsync();
		var file = await this._db.MediaFiles.FirstOrDefaultAsync(x => x.MediaFileId == mediaFileId);
		if (file is not { } mediaFile) {
			return null;
		}
		mediaFile.Rate = rate;
		this._db.Update(mediaFile);
		await this._db.SaveChangesAsync();
		await transaction.CommitAsync();
		return mediaFile;
	}

	public virtual async Task<MediaFile?> IncrementUsageCountAsync(long mediaFileId) {
		using var lockObject = await LockObjectConstants.DbLock.LockAsync();
		using var transaction = await this._db.Database.BeginTransactionAsync();
		var file = await this._db.MediaFiles.FirstOrDefaultAsync(x => x.MediaFileId == mediaFileId);
		if (file is not { } mediaFile) {
			return null;
		}
		mediaFile.UsageCount++;
		this._db.Update(mediaFile);
		await this._db.SaveChangesAsync();
		await transaction.CommitAsync();
		return mediaFile;
	}

	public virtual async Task<MediaFile?> UpdateDescriptionAsync(long mediaFileId, string description) {
		using var lockObject = await LockObjectConstants.DbLock.LockAsync();
		using var transaction = await this._db.Database.BeginTransactionAsync();
		var file = await this._db.MediaFiles.FirstOrDefaultAsync(x => x.MediaFileId == mediaFileId);
		if (file is not { } mediaFile) {
			return null;
		}
		mediaFile.Description = description;
		this._db.Update(mediaFile);
		await this._db.SaveChangesAsync();
		await transaction.CommitAsync();
		return mediaFile;
	}


	public abstract MediaType TargetMediaType {
		get;
	}

	public abstract Task<MediaFile?> RegisterFileAsync(string filePath);
}