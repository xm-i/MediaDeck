using System.Threading.Tasks;

using CommunityToolkit.Mvvm.DependencyInjection;

using MediaDeck.Composition.Enum;
using MediaDeck.Database;
using MediaDeck.Database.Tables;
using MediaDeck.FileTypes.Base.Models.Interfaces;
using MediaDeck.Models.Tools;

namespace MediaDeck.FileTypes.Base.Models;

public abstract class BaseFileOperator : IFileOperator {
	protected readonly IDbContextFactory<MediaDeckDbContext> _dbFactory;
	protected readonly FileHashUpdater _fileHashUpdater;

	protected BaseFileOperator() {
		this._dbFactory = Ioc.Default.GetRequiredService<IDbContextFactory<MediaDeckDbContext>>();
		this._fileHashUpdater = Ioc.Default.GetRequiredService<FileHashUpdater>();
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


	public abstract MediaType TargetMediaType {
		get;
	}

	public abstract Task<MediaFile?> RegisterFileAsync(string filePath);
}