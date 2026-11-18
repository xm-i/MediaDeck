using MediaDeck.Common.Base;
using MediaDeck.Composition.Database;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Composition.Objects;
using MediaDeck.Composition.Tables;
using MediaDeck.Core.Models.Files;
using MediaDeck.Core.Models.NotificationDispatcher;

using Microsoft.Extensions.Logging;

namespace MediaDeck.Core.Services.FileChangeMonitor;

/// <summary>
/// ファイルシステムの変更を監視し、DBとの同期を管理するサービスです。
/// FileSystemWatcherManager / FileChangeTracker に責務を委譲するオーケストレーターとして機能します。
/// </summary>
[Inject(InjectServiceLifetime.Singleton)]
public class FileChangeMonitorService : ModelBase {
	private readonly IDbContextFactory<MediaDeckDbContext> _dbFactory;
	private readonly IDatabaseWriteCoordinator _databaseWriteCoordinator;
	private readonly ILogger<FileChangeMonitorService> _logger;
	private readonly AppNotificationDispatcher _appNotificationDispatcher;
	private readonly FileRegistrar _fileRegistrar;
	private readonly FileSystemWatcherManager _watcherManager;
	private readonly IStringProvider _stringProvider;

	/// <summary>
	/// 未処理の変更リストを管理するトラッカー
	/// </summary>
	public FileChangeTracker Tracker {
		get;
	}

	/// <summary>
	/// FileChangeMonitorServiceクラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="dbFactory">DBコンテキストファクトリー</param>
	/// <param name="tracker">ファイル変更トラッカー</param>
	/// <param name="logger">ロガー</param>
	/// <param name="appNotificationDispatcher">通知送信クラス</param>
	/// <param name="fileRegistrar">ファイル登録クラス</param>
	/// <param name="databaseWriteCoordinator">データベース書き込み直列化サービス</param>
	/// <param name="stringProvider">文字列プロバイダー</param>
	public FileChangeMonitorService(IDbContextFactory<MediaDeckDbContext> dbFactory, FileChangeTracker tracker, ILogger<FileChangeMonitorService> logger, AppNotificationDispatcher appNotificationDispatcher, FileRegistrar fileRegistrar, IDatabaseWriteCoordinator databaseWriteCoordinator, IStringProvider stringProvider) {
		this._dbFactory = dbFactory;
		this._databaseWriteCoordinator = databaseWriteCoordinator;
		this._logger = logger;
		this._appNotificationDispatcher = appNotificationDispatcher;
		this._fileRegistrar = fileRegistrar;
		this._stringProvider = stringProvider;

		this.Tracker = tracker;
		this.Tracker.AddTo(this.CompositeDisposable);

		this._watcherManager = new FileSystemWatcherManager(logger,
			onFileDeleted: path => this.Tracker.OnDeleted(path),
			onFileRenamed: (oldPath, newPath) => this.Tracker.OnRenamed(oldPath, newPath),
			onFileCreated: path => this.Tracker.OnCreated(path),
			onFileChanged: path => this.Tracker.OnChanged(path),
			onError: this.HandleWatcherError)
			.AddTo(this.CompositeDisposable);

		// 初期フォルダーの監視を開始
		foreach (var folder in this._fileRegistrar.Config.FolderManagerConfig.Folders) {
			this._watcherManager.AddWatcher(folder.FolderPath);
		}

		// フォルダー追加の購読
		this._fileRegistrar.Config.FolderManagerConfig.Folders.ObserveAdd()
			.Subscribe(ev => {
				this._watcherManager.AddWatcher(ev.Value.FolderPath);
			})
			.AddTo(this.CompositeDisposable);

		// フォルダー削除の購読
		this._fileRegistrar.Config.FolderManagerConfig.Folders.ObserveRemove()
			.Subscribe(ev => {
				this._watcherManager.RemoveWatcher(ev.Value.FolderPath);
			})
			.AddTo(this.CompositeDisposable);
	}

	/// <summary>
	/// Watcherエラーを処理します。バッファオーバーフロー時にユーザーへ通知します。
	/// </summary>
	/// <param name="ex">発生した例外</param>
	private void HandleWatcherError(Exception ex) {
		if (ex is InternalBufferOverflowException) {
			var notif = AppNotification.Warning(
				this._stringProvider.GetString("FileChangeMonitor_BufferOverflow_Message"),
				this._stringProvider.GetString("FileChangeMonitor_BufferOverflow_Title"),
				10000);
			this._appNotificationDispatcher.Notify.OnNext(notif);
		}
	}

	/// <summary>
	/// 変更内容をデータベースに反映します。
	/// </summary>
	/// <param name="items">反映対象のアイテム一覧</param>
	/// <param name="deleteFromDb">強制的に削除するかどうか</param>
	public async Task ApplyChangesAsync(IEnumerable<FileChangeItem> items, bool deleteFromDb) {
		var targetItems = items.ToArray();
		if (targetItems.Length == 0) {
			return;
		}

		try {
			var dbUpdateItems = targetItems
				.Where(static x => x.ChangeType != FileChangeType.Added)
				.ToArray();

			var updatedPathsById = new Dictionary<long, string>();
			var deletedIds = new HashSet<long>();

			if (dbUpdateItems.Length > 0) {
				await this.ApplyDatabaseUpdatesAsync(dbUpdateItems, deleteFromDb, updatedPathsById, deletedIds).ConfigureAwait(false);
			}

			this.EnqueueAddedItems(targetItems);
			this.Tracker.NotifyChangesApplied(targetItems, updatedPathsById, deletedIds);

			if (dbUpdateItems.Length > 0) {
				FileNotifications.FilesChanged.OnNext(Unit.Default);
			}
		} catch (Exception ex) {
			this._logger.LogError(ex, "Error applying file changes to DB");
			var notif = AppNotification.Error(
				this._stringProvider.GetString("FileChangeMonitor_ApplyError_Message"),
				this._stringProvider.GetString("FileChangeMonitor_ApplyError_Title"));
			this._appNotificationDispatcher.Notify.OnNext(notif);
		}
	}

	/// <summary>
	/// 削除および移動・リネームの変更を単一トランザクション内で DB へ反映します。
	/// </summary>
	/// <param name="dbUpdateItems">DB 更新対象の変更アイテム一覧</param>
	/// <param name="deleteFromDb">強制的に削除するかどうか</param>
	/// <param name="updatedPathsById">更新されたアイテムIDと新パスの格納先辞書</param>
	/// <param name="deletedIds">削除されたアイテムIDの格納先集合</param>
	private async Task ApplyDatabaseUpdatesAsync(
		FileChangeItem[] dbUpdateItems,
		bool deleteFromDb,
		Dictionary<long, string> updatedPathsById,
		HashSet<long> deletedIds) {
		await this._databaseWriteCoordinator.ExecuteAsync(async ct => {
			await using var db = await this._dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
			await using var transaction = await db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

			var entitiesById = await LoadTargetEntitiesAsync(db, dbUpdateItems, ct).ConfigureAwait(false);
			var affectedFolderGroupOldPaths = new List<string>();
			var affectedFolderGroupNewPaths = new List<string>();
			var processedIds = new HashSet<long>();

			// 1. 削除を先に確定し、上書き移動先のユニーク制約衝突を回避する
			ApplyDeletions(db, dbUpdateItems, entitiesById, deleteFromDb, processedIds, deletedIds, affectedFolderGroupOldPaths);
			if (deletedIds.Count > 0) {
				await db.SaveChangesAsync(ct).ConfigureAwait(false);
			}

			// 2. 移動・リネームの更新（スワップや玉突き移動時は一時パスを経由してユニーク制約を回避する）
			var movedOrRenamedEntries = CollectMoveEntries(dbUpdateItems, entitiesById, deleteFromDb, processedIds);
			if (movedOrRenamedEntries.Count > 0) {
				await ApplyMovesAndRenamesAsync(
					db,
					movedOrRenamedEntries,
					affectedFolderGroupOldPaths,
					affectedFolderGroupNewPaths,
					updatedPathsById,
					ct).ConfigureAwait(false);
			}

			// 3. IsUnderFolderGroup の再計算
			await UpdateFolderGroupFlagsAfterMutationAsync(
				db,
				movedOrRenamedEntries,
				affectedFolderGroupOldPaths,
				affectedFolderGroupNewPaths,
				ct).ConfigureAwait(false);

			await transaction.CommitAsync(ct).ConfigureAwait(false);
		}).ConfigureAwait(false);
	}

	/// <summary>
	/// DB 更新対象となる <see cref="MediaItem"/> エンティティをチャンク単位で読み込みます。
	/// </summary>
	/// <param name="db">DBコンテキスト</param>
	/// <param name="dbUpdateItems">DB 更新対象の変更アイテム一覧</param>
	/// <param name="ct">キャンセルトークン</param>
	/// <returns>ID をキーとしたエンティティ辞書</returns>
	private static async Task<Dictionary<long, MediaItem>> LoadTargetEntitiesAsync(
		MediaDeckDbContext db,
		FileChangeItem[] dbUpdateItems,
		CancellationToken ct) {
		var targetIds = dbUpdateItems
			.Where(static x => x.MediaItemId.HasValue)
			.Select(static x => x.MediaItemId!.Value)
			.Distinct()
			.ToList();

		var entitiesById = new Dictionary<long, MediaItem>();
		foreach (var chunk in targetIds.Chunk(500)) {
			var chunkList = chunk.ToList();
			var loaded = await db.MediaItems
				.Where(mf => chunkList.Contains(mf.MediaItemId))
				.ToListAsync(ct)
				.ConfigureAwait(false);
			foreach (var entity in loaded) {
				entitiesById[entity.MediaItemId] = entity;
			}
		}
		return entitiesById;
	}

	/// <summary>
	/// 削除対象のエンティティを DB コンテキストから削除マークします。
	/// </summary>
	/// <param name="db">DBコンテキスト</param>
	/// <param name="dbUpdateItems">DB 更新対象の変更アイテム一覧</param>
	/// <param name="entitiesById">読み込み済みエンティティ辞書</param>
	/// <param name="deleteFromDb">強制削除フラグ</param>
	/// <param name="processedIds">処理済みID集合</param>
	/// <param name="deletedIds">削除されたID集合</param>
	/// <param name="affectedFolderGroupOldPaths">削除されたフォルダグループのパス一覧</param>
	private static void ApplyDeletions(
		MediaDeckDbContext db,
		FileChangeItem[] dbUpdateItems,
		Dictionary<long, MediaItem> entitiesById,
		bool deleteFromDb,
		HashSet<long> processedIds,
		HashSet<long> deletedIds,
		List<string> affectedFolderGroupOldPaths) {
		foreach (var item in dbUpdateItems) {
			if (!item.MediaItemId.HasValue || !entitiesById.TryGetValue(item.MediaItemId.Value, out var file)) {
				continue;
			}
			if (!deleteFromDb && item.ChangeType != FileChangeType.Deleted) {
				continue;
			}
			if (!processedIds.Add(file.MediaItemId)) {
				continue;
			}

			if (file.MediaType == MediaType.FolderGroup) {
				affectedFolderGroupOldPaths.Add(file.FilePath);
			}
			deletedIds.Add(file.MediaItemId);
			db.MediaItems.Remove(file);
		}
	}

	/// <summary>
	/// 移動またはリネーム対象となるエンティティと変更情報の組を収集します。
	/// </summary>
	/// <param name="dbUpdateItems">DB 更新対象の変更アイテム一覧</param>
	/// <param name="entitiesById">読み込み済みエンティティ辞書</param>
	/// <param name="deleteFromDb">強制削除フラグ</param>
	/// <param name="processedIds">処理済みID集合</param>
	/// <returns>移動・リネーム対象エントリ一覧</returns>
	private static List<MoveEntry> CollectMoveEntries(
		FileChangeItem[] dbUpdateItems,
		Dictionary<long, MediaItem> entitiesById,
		bool deleteFromDb,
		HashSet<long> processedIds) {
		var entries = new List<MoveEntry>();
		if (deleteFromDb) {
			return entries;
		}

		foreach (var item in dbUpdateItems) {
			if (item.ChangeType is not (FileChangeType.Moved or FileChangeType.Renamed)) {
				continue;
			}
			if (!item.MediaItemId.HasValue || !entitiesById.TryGetValue(item.MediaItemId.Value, out var file)) {
				continue;
			}
			if (!processedIds.Add(file.MediaItemId)) {
				continue;
			}

			entries.Add(new MoveEntry(file, item, file.FilePath));
		}
		return entries;
	}

	/// <summary>
	/// 移動・リネーム対象の新パスを DB へ反映します。パス衝突がある場合は一時パスを経由します。
	/// </summary>
	/// <param name="db">DBコンテキスト</param>
	/// <param name="movedOrRenamedEntries">移動・リネーム対象エントリ一覧</param>
	/// <param name="affectedFolderGroupOldPaths">影響を受けたフォルダグループ旧パス一覧</param>
	/// <param name="affectedFolderGroupNewPaths">影響を受けたフォルダグループ新パス一覧</param>
	/// <param name="updatedPathsById">更新後パスの記録先辞書</param>
	/// <param name="ct">キャンセルトークン</param>
	private static async Task ApplyMovesAndRenamesAsync(
		MediaDeckDbContext db,
		List<MoveEntry> movedOrRenamedEntries,
		List<string> affectedFolderGroupOldPaths,
		List<string> affectedFolderGroupNewPaths,
		Dictionary<long, string> updatedPathsById,
		CancellationToken ct) {
		if (HasPathCollision(movedOrRenamedEntries)) {
			foreach (var entry in movedOrRenamedEntries) {
				entry.Entity.FilePath = $"__mediadeck_swap_{entry.Entity.MediaItemId}_{Guid.NewGuid():N}__";
			}
			await db.SaveChangesAsync(ct).ConfigureAwait(false);
		}

		foreach (var entry in movedOrRenamedEntries) {
			if (entry.Entity.MediaType == MediaType.FolderGroup) {
				affectedFolderGroupOldPaths.Add(entry.OriginalFilePath);
				affectedFolderGroupNewPaths.Add(entry.ChangeItem.NewPath);
			}
			entry.Entity.FilePath = entry.ChangeItem.NewPath;
			entry.Entity.DirectoryPath = Path.GetDirectoryName(entry.ChangeItem.NewPath) ?? string.Empty;
			updatedPathsById[entry.Entity.MediaItemId] = entry.ChangeItem.NewPath;
		}
		await db.SaveChangesAsync(ct).ConfigureAwait(false);
	}

	/// <summary>
	/// 移動・リネーム群の中に、他の移動元パスと移動先パスが衝突する操作（スワップや玉突き移動）が含まれるかを判定します。
	/// </summary>
	/// <param name="entries">移動・リネーム対象エントリ一覧</param>
	/// <returns>一時パスを経由する必要がある場合は true</returns>
	private static bool HasPathCollision(List<MoveEntry> entries) {
		var oldPathSet = entries
			.Select(static e => FileChangePathUtility.NormalizePath(e.OriginalFilePath))
			.ToHashSet(StringComparer.OrdinalIgnoreCase);

		return entries.Any(e => {
			var normOld = FileChangePathUtility.NormalizePath(e.OriginalFilePath);
			var normNew = FileChangePathUtility.NormalizePath(e.ChangeItem.NewPath);
			return !string.Equals(normOld, normNew, StringComparison.OrdinalIgnoreCase)
				&& oldPathSet.Contains(normNew);
		});
	}

	/// <summary>
	/// 移動またはフォルダグループ変更に伴い、自身および配下アイテムの <see cref="MediaItem.IsUnderFolderGroup"/> フラグを再計算します。
	/// </summary>
	/// <param name="db">DBコンテキスト</param>
	/// <param name="movedOrRenamedEntries">移動・リネーム対象エントリ一覧</param>
	/// <param name="affectedFolderGroupOldPaths">影響を受けたフォルダグループ旧パス一覧</param>
	/// <param name="affectedFolderGroupNewPaths">影響を受けたフォルダグループ新パス一覧</param>
	/// <param name="ct">キャンセルトークン</param>
	private static async Task UpdateFolderGroupFlagsAfterMutationAsync(
		MediaDeckDbContext db,
		List<MoveEntry> movedOrRenamedEntries,
		List<string> affectedFolderGroupOldPaths,
		List<string> affectedFolderGroupNewPaths,
		CancellationToken ct) {
		if (movedOrRenamedEntries.Count == 0 && affectedFolderGroupOldPaths.Count == 0 && affectedFolderGroupNewPaths.Count == 0) {
			return;
		}

		var allFolderGroupPaths = await db.MediaItems
			.AsNoTracking()
			.Where(x => x.MediaType == MediaType.FolderGroup)
			.Select(x => x.FilePath)
			.ToListAsync(ct)
			.ConfigureAwait(false);

		await UpdateMovedEntriesFolderGroupFlagAsync(db, movedOrRenamedEntries, allFolderGroupPaths, ct).ConfigureAwait(false);

		var impactedPrefixes = affectedFolderGroupOldPaths
			.Concat(affectedFolderGroupNewPaths)
			.Select(FileChangePathUtility.NormalizePath)
			.Where(static p => !string.IsNullOrEmpty(p))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();

		if (impactedPrefixes.Count == 0) {
			return;
		}

		var movedIds = movedOrRenamedEntries.Select(static x => x.Entity.MediaItemId).ToHashSet();
		var (setTrueIds, setFalseIds) = await CollectImpactedChildFlagChangesAsync(
			db,
			impactedPrefixes,
			movedIds,
			allFolderGroupPaths,
			ct).ConfigureAwait(false);

		await ExecuteFolderGroupFlagUpdatesAsync(db, setTrueIds, true, ct).ConfigureAwait(false);
		await ExecuteFolderGroupFlagUpdatesAsync(db, setFalseIds, false, ct).ConfigureAwait(false);
	}

	/// <summary>
	/// 移動・リネームされたエンティティ自身の <see cref="MediaItem.IsUnderFolderGroup"/> フラグを更新します。
	/// </summary>
	/// <param name="db">DBコンテキスト</param>
	/// <param name="movedOrRenamedEntries">移動・リネーム対象エントリ一覧</param>
	/// <param name="allFolderGroupPaths">現在登録されているすべてのフォルダグループパス</param>
	/// <param name="ct">キャンセルトークン</param>
	private static async Task UpdateMovedEntriesFolderGroupFlagAsync(
		MediaDeckDbContext db,
		List<MoveEntry> movedOrRenamedEntries,
		List<string> allFolderGroupPaths,
		CancellationToken ct) {
		var movedStatusChanged = false;
		foreach (var entry in movedOrRenamedEntries) {
			var isUnder = IsPathUnderAnyFolderGroup(entry.Entity.DirectoryPath, allFolderGroupPaths);
			if (entry.Entity.IsUnderFolderGroup != isUnder) {
				entry.Entity.IsUnderFolderGroup = isUnder;
				movedStatusChanged = true;
			}
		}
		if (movedStatusChanged) {
			await db.SaveChangesAsync(ct).ConfigureAwait(false);
		}
	}

	/// <summary>
	/// 変更されたフォルダグループ配下にある子アイテムについて、フラグ更新が必要な ID を抽出します。
	/// </summary>
	/// <param name="db">DBコンテキスト</param>
	/// <param name="impactedPrefixes">影響を受けたフォルダグループパス一覧</param>
	/// <param name="movedIds">移動済みエンティティのID集合（除外対象）</param>
	/// <param name="allFolderGroupPaths">現在登録されているすべてのフォルダグループパス</param>
	/// <param name="ct">キャンセルトークン</param>
	/// <returns>true へ更新する ID 一覧と false へ更新する ID 一覧のタプル</returns>
	private static async Task<(List<long> SetTrueIds, List<long> SetFalseIds)> CollectImpactedChildFlagChangesAsync(
		MediaDeckDbContext db,
		List<string> impactedPrefixes,
		HashSet<long> movedIds,
		List<string> allFolderGroupPaths,
		CancellationToken ct) {
		var setTrueIds = new List<long>();
		var setFalseIds = new List<long>();

		if (impactedPrefixes.Count <= 5) {
			var seenIds = new HashSet<long>();
			foreach (var prefix in impactedPrefixes) {
				var (sepBack, sepFwd) = FileChangePathUtility.CreatePrefixPatterns(prefix);
				var candidates = await db.MediaItems
					.AsNoTracking()
					.Where(x => x.DirectoryPath == prefix
						|| x.DirectoryPath.StartsWith(sepBack)
						|| x.DirectoryPath.StartsWith(sepFwd))
					.Select(x => new { x.MediaItemId, x.DirectoryPath, x.IsUnderFolderGroup })
					.ToListAsync(ct)
					.ConfigureAwait(false);

				foreach (var candidate in candidates) {
					if (movedIds.Contains(candidate.MediaItemId) || !seenIds.Add(candidate.MediaItemId)) {
						continue;
					}
					ClassifyChildFlagChange(
						candidate.MediaItemId,
						candidate.DirectoryPath,
						candidate.IsUnderFolderGroup,
						allFolderGroupPaths,
						setTrueIds,
						setFalseIds);
				}
			}
			return (setTrueIds, setFalseIds);
		}

		var allCandidates = await db.MediaItems
			.AsNoTracking()
			.Select(x => new { x.MediaItemId, x.DirectoryPath, x.IsUnderFolderGroup })
			.ToListAsync(ct)
			.ConfigureAwait(false);

		foreach (var candidate in allCandidates) {
			if (movedIds.Contains(candidate.MediaItemId)) {
				continue;
			}
			if (!impactedPrefixes.Any(prefix => FileChangePathUtility.IsSelfOrDescendant(candidate.DirectoryPath, prefix))) {
				continue;
			}
			ClassifyChildFlagChange(
				candidate.MediaItemId,
				candidate.DirectoryPath,
				candidate.IsUnderFolderGroup,
				allFolderGroupPaths,
				setTrueIds,
				setFalseIds);
		}

		return (setTrueIds, setFalseIds);
	}

	/// <summary>
	/// 候補アイテムの現在の <see cref="MediaItem.IsUnderFolderGroup"/> と期待値を比較し、更新先リストへ振り分けます。
	/// </summary>
	/// <param name="mediaItemId">メディアアイテムID</param>
	/// <param name="directoryPath">アイテムのディレクトリパス</param>
	/// <param name="currentFlag">現在のフラグ値</param>
	/// <param name="allFolderGroupPaths">すべてのフォルダグループパス</param>
	/// <param name="setTrueIds">true へ更新するID一覧</param>
	/// <param name="setFalseIds">false へ更新するID一覧</param>
	private static void ClassifyChildFlagChange(
		long mediaItemId,
		string directoryPath,
		bool currentFlag,
		List<string> allFolderGroupPaths,
		List<long> setTrueIds,
		List<long> setFalseIds) {
		var expected = IsPathUnderAnyFolderGroup(directoryPath, allFolderGroupPaths);
		if (currentFlag == expected) {
			return;
		}
		if (expected) {
			setTrueIds.Add(mediaItemId);
		} else {
			setFalseIds.Add(mediaItemId);
		}
	}

	/// <summary>
	/// 指定された ID 群の <see cref="MediaItem.IsUnderFolderGroup"/> フラグをチャンク単位で一括更新します。
	/// </summary>
	/// <param name="db">DBコンテキスト</param>
	/// <param name="ids">更新対象のメディアアイテムID一覧</param>
	/// <param name="value">設定するフラグ値</param>
	/// <param name="ct">キャンセルトークン</param>
	private static async Task ExecuteFolderGroupFlagUpdatesAsync(
		MediaDeckDbContext db,
		List<long> ids,
		bool value,
		CancellationToken ct) {
		foreach (var chunk in ids.Chunk(500)) {
			var chunkList = chunk.ToList();
			await db.MediaItems
				.Where(x => chunkList.Contains(x.MediaItemId))
				.ExecuteUpdateAsync(s => s.SetProperty(m => m.IsUnderFolderGroup, value), ct)
				.ConfigureAwait(false);
		}
	}

	/// <summary>
	/// 新規追加されたアイテムをファイル登録キューへ投入します。フォルダグループ登録前に配下ファイルを優先します。
	/// </summary>
	/// <param name="targetItems">反映対象のアイテム一覧</param>
	private void EnqueueAddedItems(FileChangeItem[] targetItems) {
		// フォルダ(FolderGroup)登録時に配下ファイルのサムネイルを利用できるよう、通常ファイルを先にキューへ投入する
		var addedItems = targetItems
			.Where(static x => x.ChangeType == FileChangeType.Added)
			.OrderBy(static x => Directory.Exists(x.NewPath) ? 1 : 0);
		foreach (var item in addedItems) {
			this._fileRegistrar.Enqueue(item.NewPath);
		}
	}

	/// <summary>
	/// 指定されたディレクトリパスがいずれかのフォルダグループ配下にあるか判定します。
	/// </summary>
	/// <param name="directoryPath">判定対象のディレクトリパス</param>
	/// <param name="folderGroupPaths">登録済みフォルダグループのパス一覧</param>
	/// <returns>フォルダグループ配下である場合は true</returns>
	private static bool IsPathUnderAnyFolderGroup(string directoryPath, IEnumerable<string> folderGroupPaths) {
		if (string.IsNullOrEmpty(directoryPath)) {
			return false;
		}
		foreach (var fgPath in folderGroupPaths) {
			if (FileChangePathUtility.IsSelfOrDescendant(directoryPath, fgPath)) {
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// 未処理変更として提示された内容を破棄（無視）します。
	/// </summary>
	/// <param name="items">破棄対象のアイテム一覧</param>
	public void DiscardChanges(IEnumerable<FileChangeItem> items) {
		this.Tracker.RemoveItems(items);
	}

	/// <summary>
	/// DB 更新時の移動・リネーム処理で元のパスを保持するための作業エントリです。
	/// </summary>
	/// <param name="Entity">更新対象のメディアアイテムエンティティ</param>
	/// <param name="ChangeItem">変更内容を表すアイテム</param>
	/// <param name="OriginalFilePath">変更前の元のファイルパス</param>
	private sealed record MoveEntry(MediaItem Entity, FileChangeItem ChangeItem, string OriginalFilePath);
}