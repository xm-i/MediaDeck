using MediaDeck.Common.Base;
using MediaDeck.Composition.Database;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using Microsoft.Extensions.Logging;

namespace MediaDeck.Core.Services.MediaItemMetadataUpdator;

/// <summary>
/// メディアアイテムのメタデータを更新するサービス。
/// </summary>
[Inject(InjectServiceLifetime.Singleton)]
public class MediaItemMetadataUpdatorService : ServiceBase {
	private readonly IDbContextFactory<MediaDeckDbContext> _dbFactory;
	private readonly IMediaItemTypeService _mediaItemTypeService;
	private readonly ILogger<MediaItemMetadataUpdatorService>? _logger;

	/// <summary>
	/// 更新対象の総数。
	/// </summary>
	public ReactiveProperty<long> TargetCount {
		get;
	}

	/// <summary>
	/// 更新完了数。
	/// </summary>
	public ReactiveProperty<long> CompletedCount {
		get;
	}

	/// <summary>
	/// コンストラクタ。
	/// </summary>
	/// <param name="dbFactory">DbContextファクトリ</param>
	/// <param name="mediaItemTypeService">メディアアイテム種別サービス</param>
	/// <param name="logger">ロガー（省略可）</param>
	public MediaItemMetadataUpdatorService(
		IDbContextFactory<MediaDeckDbContext> dbFactory,
		IMediaItemTypeService mediaItemTypeService,
		ILogger<MediaItemMetadataUpdatorService>? logger = null) {
		this._dbFactory = dbFactory;
		this._mediaItemTypeService = mediaItemTypeService;
		this._logger = logger;

		this.TargetCount = new ReactiveProperty<long>().AddTo(this.CompositeDisposable);
		this.CompletedCount = new ReactiveProperty<long>().AddTo(this.CompositeDisposable);
	}

	/// <summary>
	/// 全てのメディアアイテムのメタデータを更新する。
	/// </summary>
	/// <param name="ct">キャンセル・トークン</param>
	/// <returns>タスク</returns>
	public async Task UpdateMetadataAsync(CancellationToken ct = default) {
		List<long> targetIds;
		await using (var db = await this._dbFactory.CreateDbContextAsync(ct)) {
			targetIds = await db.MediaItems.Select(x => x.MediaItemId).ToListAsync(ct);
		}

		await this.UpdateMetadataAsync(targetIds, ct);
	}

	/// <summary>
	/// 指定されたメディアアイテムのメタデータを更新する。
	/// </summary>
	/// <param name="ids">更新対象のIDリスト</param>
	/// <param name="ct">キャンセル・トークン</param>
	/// <returns>タスク</returns>
	public async Task UpdateMetadataAsync(IEnumerable<long> ids, CancellationToken ct = default) {
		var targetIds = ids.ToList();
		if (this.CompletedCount.Value < this.TargetCount.Value) {
			this.TargetCount.Value += targetIds.Count;
		} else {
			this.TargetCount.Value = targetIds.Count;
			this.CompletedCount.Value = 0;
		}

		var operators = this._mediaItemTypeService.CreateMediaItemOperators().ToDictionary(x => x.TargetMediaType);

		foreach (var id in targetIds) {
			if (ct.IsCancellationRequested) {
				return;
			}

			try {
				await using var db = await this._dbFactory.CreateDbContextAsync(ct);
				var file = await this._mediaItemTypeService.IncludeTables(db.MediaItems).FirstOrDefaultAsync(x => x.MediaItemId == id, ct);

				if (file != null) {
					if (operators.TryGetValue(file.MediaType, out var op)) {
						await op.UpdateMetadata(file);
					}
					await db.SaveChangesAsync(ct);
				}
			} catch (OperationCanceledException) {
				return;
			} catch (Exception ex) {
				this._logger?.LogError(ex, "Failed to update metadata for MediaItemId {MediaItemId}", id);
			} finally {
				this.CompletedCount.Value++;
			}
		}
	}
}