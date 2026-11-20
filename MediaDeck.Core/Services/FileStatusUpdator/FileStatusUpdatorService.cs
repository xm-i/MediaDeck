using MediaDeck.Common.Base;
using MediaDeck.Composition.Database;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;

namespace MediaDeck.Core.Services.FileStatusUpdator;

/// <summary>
/// ファイルシステム上の状態をデータベースのメディアアイテム情報へ反映するサービスです。
/// </summary>
[Inject(InjectServiceLifetime.Transient)]
public class FileStatusUpdatorService : ServiceBase {
	/// <summary>
	/// FileStatusUpdatorServiceクラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="dbFactory">データベースコンテキストファクトリー</param>
	/// <param name="fileHashUpdatorService">ファイルハッシュ更新サービス</param>
	/// <param name="mediaItemTypeService">メディアアイテム種別サービス</param>
	/// <param name="databaseWriteCoordinator">データベース書き込み直列化サービス</param>
	public FileStatusUpdatorService(IDbContextFactory<MediaDeckDbContext> dbFactory, IFileHashUpdatorService fileHashUpdatorService, IMediaItemTypeService mediaItemTypeService, IDatabaseWriteCoordinator databaseWriteCoordinator) {
		this._dbFactory = dbFactory;
		this._fileHashUpdatorService = fileHashUpdatorService;
		this._mediaItemTypeService = mediaItemTypeService;
		this._databaseWriteCoordinator = databaseWriteCoordinator;
	}

	private readonly IDbContextFactory<MediaDeckDbContext> _dbFactory;
	private readonly IFileHashUpdatorService _fileHashUpdatorService;
	private readonly IMediaItemTypeService _mediaItemTypeService;
	private readonly IDatabaseWriteCoordinator _databaseWriteCoordinator;

	/// <summary>
	/// 更新対象の総数です。
	/// </summary>
	public ReactiveProperty<long> TargetCount {
		get;
	} = new();

	/// <summary>
	/// 更新確認が完了した件数です。
	/// </summary>
	public ReactiveProperty<long> CompletedCount {
		get;
	} = new();

	/// <summary>
	/// ファイルシステム上の状態を確認し、必要なデータベース更新とハッシュ更新キューへの追加を行います。
	/// </summary>
	/// <param name="ct">キャンセルトークン</param>
	public async Task UpdateFileInfo(CancellationToken ct = default) {
		// 1. 全アイテムの情報を一気にメモリに読み込む
		List<MediaItemBasicInfo> items;
		await using (var db = await this._dbFactory.CreateDbContextAsync(ct)) {
			items = await db.MediaItems
				.Select(x => new MediaItemBasicInfo(
					x.MediaItemId,
					x.FilePath,
					x.MediaType,
					x.IsExists,
					x.FileSize,
					x.CreationTime,
					x.ModifiedTime,
					x.LastAccessTime,
					x.PreHashUpdatedTime
				))
				.AsNoTracking()
				.ToListAsync(ct);
		}

		this.TargetCount.Value = items.Count;
		this.CompletedCount.Value = 0;

		var updates = new List<FileStatusUpdateInfo>();

		// 2. 状態チェックフェーズ (DBアクセスなし、ディスクI/Oのみ)
		foreach (var item in items) {
			if (ct.IsCancellationRequested) {
				return;
			}

			var mediaItemTypeProvider = this._mediaItemTypeService.GetMediaItemTypeProvider(item.MediaType);
			var pathStatus = mediaItemTypeProvider.GetPathStatus(item.FilePath);

			if (
				item.IsExists == pathStatus.Exists &&
				(!item.IsExists ||
					(
						item.FileSize == pathStatus.FileSize &&
						item.CreationTime == pathStatus.CreationTime &&
						item.ModifiedTime == pathStatus.ModifiedTime &&
						item.LastAccessTime == pathStatus.LastAccessTime &&
						item.PreHashUpdatedTime != null &&
						item.PreHashUpdatedTime >= pathStatus.ModifiedTime
					)
				)
			) {
				this.CompletedCount.Value++;
				continue;
			}

			var needsHashUpdate = pathStatus.Exists && (item.PreHashUpdatedTime == null || item.PreHashUpdatedTime < pathStatus.ModifiedTime) && item.MediaType != MediaType.FolderGroup;

			if (needsHashUpdate) {
				this._fileHashUpdatorService.EnqueueHashUpdate(item.MediaItemId);
			}

			updates.Add(new FileStatusUpdateInfo(
				item.MediaItemId,
				pathStatus.Exists,
				pathStatus.Exists ? pathStatus.FileSize : item.FileSize,
				pathStatus.Exists ? pathStatus.CreationTime : item.CreationTime,
				pathStatus.Exists ? pathStatus.ModifiedTime : item.ModifiedTime,
				pathStatus.Exists ? pathStatus.LastAccessTime : item.LastAccessTime
			));
			this.CompletedCount.Value++;
		}

		// 3. 更新フェーズ (ExecuteUpdate を使用してフェッチなしで直接更新)
		if (updates.Count > 0) {
			foreach (var updateChunk in updates.Chunk(100)) {
				if (ct.IsCancellationRequested) {
					return;
				}

				await this._databaseWriteCoordinator.ExecuteAsync(async writeCt => {
					await using var db = await this._dbFactory.CreateDbContextAsync(writeCt);
					await using var transaction = await db.Database.BeginTransactionAsync(writeCt);
					foreach (var info in updateChunk) {
						if (info.IsExists) {
							await db.MediaItems
								.Where(x => x.MediaItemId == info.MediaItemId)
								.ExecuteUpdateAsync(s => s
									.SetProperty(m => m.IsExists, info.IsExists)
									.SetProperty(m => m.FileSize, info.FileSize)
									.SetProperty(m => m.CreationTime, info.CreationTime)
									.SetProperty(m => m.ModifiedTime, info.ModifiedTime)
									.SetProperty(m => m.LastAccessTime, info.LastAccessTime),
								writeCt);
						} else {
							await db.MediaItems
								.Where(x => x.MediaItemId == info.MediaItemId)
								.ExecuteUpdateAsync(s => s
									.SetProperty(m => m.IsExists, info.IsExists),
								writeCt);
						}
					}
					await transaction.CommitAsync(writeCt);
				}, ct);
			}
		}


		// PreHash更新がなかった場合もFullHashのチェックを行う
		await this._fileHashUpdatorService.CheckAndEnqueueFullHashUpdatesAsync(ct);
	}

	private record FileStatusUpdateInfo(
		long MediaItemId,
		bool IsExists,
		long FileSize,
		DateTime CreationTime,
		DateTime ModifiedTime,
		DateTime LastAccessTime
	);

	private record MediaItemBasicInfo(
		long MediaItemId,
		string FilePath,
		MediaType MediaType,
		bool IsExists,
		long FileSize,
		DateTime CreationTime,
		DateTime ModifiedTime,
		DateTime LastAccessTime,
		DateTime? PreHashUpdatedTime
	);
}