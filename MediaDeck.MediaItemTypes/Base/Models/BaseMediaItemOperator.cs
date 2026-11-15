using System.IO;
using System.Threading.Tasks;
using MediaDeck.Composition.Database;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;
using MediaDeck.Composition.Tables;

namespace MediaDeck.MediaItemTypes.Base.Models;

/// <summary>
/// メディアアイテム操作の基底クラス
/// </summary>
public abstract class BaseMediaItemOperator : IMediaItemOperator {
	protected readonly IDbContextFactory<MediaDeckDbContext> _dbFactory;
	protected readonly IFileHashUpdatorService _updateFileHashBackgroundService;
	protected readonly IDatabaseWriteCoordinator _databaseWriteCoordinator;

	/// <summary>
	/// コンストラクタ
	/// </summary>
	/// <param name="dbFactory">DbContextファクトリ</param>
	/// <param name="updateFileHashBackgroundService">ハッシュ更新サービス</param>
	/// <param name="databaseWriteCoordinator">データベース書き込みコーディネーター</param>
	/// <param name="targetMediaType">対象メディア種別</param>
	protected BaseMediaItemOperator(
		IDbContextFactory<MediaDeckDbContext> dbFactory,
		IFileHashUpdatorService updateFileHashBackgroundService,
		IDatabaseWriteCoordinator databaseWriteCoordinator,
		MediaType targetMediaType) {
		this._dbFactory = dbFactory;
		this._updateFileHashBackgroundService = updateFileHashBackgroundService;
		this._databaseWriteCoordinator = databaseWriteCoordinator;
		this.TargetMediaType = targetMediaType;
	}

	/// <summary>
	/// 評価値を更新します。
	/// </summary>
	/// <param name="MediaItemId">メディアアイテムID</param>
	/// <param name="rate">評価値</param>
	/// <returns>更新されたメディアアイテム</returns>
	public virtual async Task<MediaItem?> UpdateRateAsync(long MediaItemId, int rate) {
		return await this._databaseWriteCoordinator.ExecuteAsync(async ct => {
			await using var db = await this._dbFactory.CreateDbContextAsync(ct);
			using var transaction = await db.Database.BeginTransactionAsync(ct);
			var file = await db.MediaItems.FirstOrDefaultAsync(x => x.MediaItemId == MediaItemId, ct);
			if (file is not { } mediaItem) {
				return null;
			}
			mediaItem.Rate = rate;
			db.Update(mediaItem);
			await db.SaveChangesAsync(ct);
			await transaction.CommitAsync(ct);
			return mediaItem;
		});
	}

	/// <summary>
	/// 使用回数をインクリメントします。
	/// </summary>
	/// <param name="MediaItemId">メディアアイテムID</param>
	/// <returns>更新されたメディアアイテム</returns>
	public virtual async Task<MediaItem?> IncrementUsageCountAsync(long MediaItemId) {
		return await this._databaseWriteCoordinator.ExecuteAsync(async ct => {
			await using var db = await this._dbFactory.CreateDbContextAsync(ct);
			using var transaction = await db.Database.BeginTransactionAsync(ct);
			var file = await db.MediaItems.FirstOrDefaultAsync(x => x.MediaItemId == MediaItemId, ct);
			if (file is not { } mediaItem) {
				return null;
			}
			mediaItem.UsageCount++;
			db.Update(mediaItem);
			await db.SaveChangesAsync(ct);
			await transaction.CommitAsync(ct);
			return mediaItem;
		});
	}

	/// <summary>
	/// 説明文を更新します。
	/// </summary>
	/// <param name="MediaItemId">メディアアイテムID</param>
	/// <param name="description">説明文</param>
	/// <returns>更新されたメディアアイテム</returns>
	public virtual async Task<MediaItem?> UpdateDescriptionAsync(long MediaItemId, string description) {
		return await this._databaseWriteCoordinator.ExecuteAsync(async ct => {
			await using var db = await this._dbFactory.CreateDbContextAsync(ct);
			using var transaction = await db.Database.BeginTransactionAsync(ct);
			var file = await db.MediaItems.FirstOrDefaultAsync(x => x.MediaItemId == MediaItemId, ct);
			if (file is not { } mediaItem) {
				return null;
			}
			mediaItem.Description = description;
			db.Update(mediaItem);
			await db.SaveChangesAsync(ct);
			await transaction.CommitAsync(ct);
			return mediaItem;
		});
	}

	public virtual Task UpdateMetadata(MediaItem mediaItem) {
		return Task.CompletedTask;
	}

	public MediaType TargetMediaType {
		get;
	}

	protected async Task<bool> GetIsUnderFolderGroup(MediaDeckDbContext dbContext, string directoryPath) {
		return await dbContext.MediaItems.AnyAsync(x => directoryPath == x.FilePath || directoryPath.StartsWith(x.FilePath + Path.DirectorySeparatorChar));
	}

	public abstract Task<MediaItem?> RegisterMediaItemAsync(string filePath);
}