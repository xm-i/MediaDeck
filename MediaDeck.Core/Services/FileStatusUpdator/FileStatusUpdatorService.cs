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
	public FileStatusUpdatorService(
		IDbContextFactory<MediaDeckDbContext> dbFactory,
		IFileHashUpdatorService fileHashUpdatorService,
		IMediaItemTypeService mediaItemTypeService,
		IDatabaseWriteCoordinator databaseWriteCoordinator) {
		this._dbFactory = dbFactory;
		this._fileHashUpdatorService = fileHashUpdatorService;
		this._mediaItemTypeService = mediaItemTypeService;
		this._databaseWriteCoordinator = databaseWriteCoordinator;

		this.TargetCount = new ReactiveProperty<long>().AddTo(this.CompositeDisposable);
		this.CompletedCount = new ReactiveProperty<long>().AddTo(this.CompositeDisposable);
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
	}

	/// <summary>
	/// 更新確認が完了した件数です。
	/// </summary>
	public ReactiveProperty<long> CompletedCount {
		get;
	}

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
		var hashUpdateIds = new List<long>();

		// 2. 状態チェックフェーズ (DBアクセスなし、ディスクI/Oのみ)
		foreach (var item in items) {
			if (ct.IsCancellationRequested) {
				return;
			}

			var mediaItemTypeProvider = this._mediaItemTypeService.GetMediaItemTypeProvider(item.MediaType);
			var pathStatus = mediaItemTypeProvider.GetPathStatus(item.FilePath);

			if (HasStatusChanged(item, pathStatus)) {
				updates.Add(new FileStatusUpdateInfo(
					item.MediaItemId,
					item.FilePath,
					pathStatus.Exists,
					pathStatus.Exists ? pathStatus.FileSize : item.FileSize,
					pathStatus.Exists ? pathStatus.CreationTime : item.CreationTime,
					pathStatus.Exists ? pathStatus.ModifiedTime : item.ModifiedTime,
					pathStatus.Exists ? pathStatus.LastAccessTime : item.LastAccessTime
				));
			}

			if (NeedsHashUpdate(item, pathStatus)) {
				hashUpdateIds.Add(item.MediaItemId);
			}

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
					await ApplyUpdateChunkAsync(db, updateChunk, writeCt);
					await transaction.CommitAsync(writeCt);
				}, ct);
			}
		}

		if (ct.IsCancellationRequested) {
			return;
		}

		foreach (var mediaItemId in hashUpdateIds) {
			this._fileHashUpdatorService.EnqueueHashUpdate(mediaItemId);
		}

		// PreHash更新がなかった場合もFullHashのチェックを行う
		await this._fileHashUpdatorService.CheckAndEnqueueFullHashUpdatesAsync(ct);
	}

	/// <summary>
	/// データベース上のファイル状態とファイルシステム上の状態に差分があるかどうかを判定します。
	/// </summary>
	/// <param name="item">データベース上のアイテム情報</param>
	/// <param name="pathStatus">ファイルシステム上の状態</param>
	/// <returns>更新が必要な場合は true、それ以外は false</returns>
	private static bool HasStatusChanged(MediaItemBasicInfo item, MediaItemPathStatus pathStatus) {
		if (item.IsExists != pathStatus.Exists) {
			return true;
		}

		if (!pathStatus.Exists) {
			return false;
		}

		return item.FileSize != pathStatus.FileSize
			|| item.CreationTime != pathStatus.CreationTime
			|| item.ModifiedTime != pathStatus.ModifiedTime
			|| item.LastAccessTime != pathStatus.LastAccessTime;
	}

	/// <summary>
	/// ハッシュ値（PreHash）の再計算をキューへ追加する必要があるかどうかを判定します。
	/// </summary>
	/// <param name="item">データベース上のアイテム情報</param>
	/// <param name="pathStatus">ファイルシステム上の状態</param>
	/// <returns>ハッシュ更新が必要な場合は true、それ以外は false</returns>
	private static bool NeedsHashUpdate(MediaItemBasicInfo item, MediaItemPathStatus pathStatus) {
		return pathStatus.Exists
			&& item.MediaType != MediaType.FolderGroup
			&& (item.PreHashUpdatedTime == null || item.PreHashUpdatedTime < pathStatus.ModifiedTime);
	}

	/// <summary>
	/// 1チャンク分のファイル状態更新をデータベースへ適用します。
	/// スキャン中に別処理でリネームされたアイテムを誤って上書きしないよう、現在のFilePathが一致するもののみ更新します。
	/// </summary>
	/// <param name="db">データベースコンテキスト</param>
	/// <param name="updateChunk">更新対象チャンク</param>
	/// <param name="ct">キャンセルトークン</param>
	private static async Task ApplyUpdateChunkAsync(MediaDeckDbContext db, FileStatusUpdateInfo[] updateChunk, CancellationToken ct) {
		foreach (var info in updateChunk.Where(x => x.IsExists)) {
			await db.MediaItems
				.Where(x => x.MediaItemId == info.MediaItemId && x.FilePath == info.FilePath)
				.ExecuteUpdateAsync(s => s
					.SetProperty(m => m.IsExists, info.IsExists)
					.SetProperty(m => m.FileSize, info.FileSize)
					.SetProperty(m => m.CreationTime, info.CreationTime)
					.SetProperty(m => m.ModifiedTime, info.ModifiedTime)
					.SetProperty(m => m.LastAccessTime, info.LastAccessTime),
				ct);
		}

		var missingItems = updateChunk.Where(x => !x.IsExists).ToArray();
		if (missingItems.Length > 0) {
			var expectedPathById = missingItems.ToDictionary(x => x.MediaItemId, x => x.FilePath);
			var missingIds = expectedPathById.Keys.ToArray();

			var currentItems = await db.MediaItems
				.AsNoTracking()
				.Where(x => missingIds.Contains(x.MediaItemId))
				.Select(x => new { x.MediaItemId, x.FilePath })
				.ToListAsync(ct);

			var unchangedMissingIds = currentItems
				.Where(x => expectedPathById.TryGetValue(x.MediaItemId, out var expectedPath)
					&& string.Equals(x.FilePath, expectedPath, StringComparison.OrdinalIgnoreCase))
				.Select(x => x.MediaItemId)
				.ToArray();

			if (unchangedMissingIds.Length > 0) {
				await db.MediaItems
					.Where(x => unchangedMissingIds.Contains(x.MediaItemId))
					.ExecuteUpdateAsync(s => s
						.SetProperty(m => m.IsExists, false),
					ct);
			}
		}
	}

	private record FileStatusUpdateInfo(
		long MediaItemId,
		string FilePath,
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