using MediaDeck.Common.Utilities;
using MediaDeck.Composition.Database;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Composition.Tables;

using Microsoft.Extensions.Logging;

namespace MediaDeck.Core.Services.FileChangeMonitor;

/// <summary>
/// 累積されたリネーム操作を表す値型です。
/// </summary>
/// <param name="OldPath">変更前の正規化パス</param>
/// <param name="NewPath">変更後の正規化パス</param>
internal readonly record struct RenameOperation(string OldPath, string NewPath);

/// <summary>
/// 差分計算（Reconcile）への入力スナップショットです。
/// </summary>
/// <param name="DirtyPaths">変更通知を受けたパス集合</param>
/// <param name="RenameHistory">時系列のリネーム履歴</param>
/// <param name="ScopedExistingItems">再評価対象となる既存未処理アイテム</param>
/// <param name="UntouchedExistingItems">今回の評価対象外として維持する既存未処理アイテム</param>
internal sealed record ReconcileInput(
	HashSet<string> DirtyPaths,
	List<RenameOperation> RenameHistory,
	List<FileChangeItem> ScopedExistingItems,
	List<FileChangeItem> UntouchedExistingItems);

/// <summary>
/// 差分計算（Reconcile）の結果を表します。
/// </summary>
/// <param name="ReconciledItems">確定した変更アイテム一覧</param>
/// <param name="UpgradedUntouchedItems">クロスバッチで Moved へ昇格した既存アイテムの置換ペア</param>
/// <param name="RetryPaths">I/O ロック等により再試行が必要なパス一覧</param>
/// <param name="RetryRenames">再試行時に引き継ぐリネーム操作一覧</param>
internal sealed record ReconcileResult(
	List<FileChangeItem> ReconciledItems,
	List<(FileChangeItem OldItem, FileChangeItem NewItem)> UpgradedUntouchedItems,
	List<string> RetryPaths,
	List<RenameOperation> RetryRenames);

/// <summary>
/// データベース上の登録状態とファイルシステムの実態を比較し、確定したファイル変更差分を算出するクラスです。
/// </summary>
internal sealed class FileChangeReconciler {
	private readonly IDbContextFactory<MediaDeckDbContext> _dbFactory;
	private readonly ConfigModel _config;
	private readonly ILogger _logger;
	private readonly Func<string, bool> _tryConsumeRetrySlot;
	private readonly Action<string> _resetRetryCount;

	/// <summary>
	/// <see cref="FileChangeReconciler"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="dbFactory">DBコンテキストファクトリー</param>
	/// <param name="config">設定モデル</param>
	/// <param name="logger">ロガー</param>
	/// <param name="tryConsumeRetrySlot">リトライ回数消費コールバック</param>
	/// <param name="resetRetryCount">リトライ回数リセットコールバック</param>
	public FileChangeReconciler(
		IDbContextFactory<MediaDeckDbContext> dbFactory,
		ConfigModel config,
		ILogger logger,
		Func<string, bool> tryConsumeRetrySlot,
		Action<string> resetRetryCount) {
		this._dbFactory = dbFactory;
		this._config = config;
		this._logger = logger;
		this._tryConsumeRetrySlot = tryConsumeRetrySlot;
		this._resetRetryCount = resetRetryCount;
	}

	/// <summary>
	/// DBとディスク実態の差分計算を実行します。
	/// </summary>
	/// <param name="input">差分計算への入力スナップショット</param>
	/// <param name="cancellationToken">キャンセルトークン</param>
	/// <returns>算出された変更差分およびリトライ情報</returns>
	public async Task<ReconcileResult> ReconcileAsync(ReconcileInput input, CancellationToken cancellationToken) {
		var context = this.BuildSearchContext(input);

		await using var db = await this._dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
		var dbItemsById = await this.FetchAffectedDbItemsAsync(db, context.CandidateOldPaths, cancellationToken).ConfigureAwait(false);
		var resolvedPathById = BuildResolvedTargetPaths(dbItemsById, context, input.RenameHistory);

		var diskFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var diskFolderGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		this.CollectDiskEntries(context.CandidateNewPaths, context.UntouchedNewPaths, diskFiles, diskFolderGroups);

		var classification = this.ClassifyDbItems(dbItemsById, resolvedPathById, context, diskFiles, diskFolderGroups);

		var remainingDiskFiles = await this.ExcludeRegisteredAndOverwrittenFilesAsync(
			db,
			diskFiles,
			diskFolderGroups,
			classification.EvaluatedDbPaths,
			classification.MissingDbItems,
			cancellationToken).ConfigureAwait(false);

		var addedEvaluation = this.EvaluateAddedAndCrossBatchMoves(
			remainingDiskFiles,
			diskFolderGroups,
			classification.MissingDbItems,
			input.UntouchedExistingItems,
			classification.ReconciledItems,
			classification.RetryPaths,
			cancellationToken);

		EvaluateRemainingMissingItems(
			classification.MissingDbItems,
			input.UntouchedExistingItems,
			addedEvaluation.MatchedUntouchedItems,
			addedEvaluation.UpgradedUntouchedItems,
			classification.ReconciledItems);

		return new ReconcileResult(
			classification.ReconciledItems,
			addedEvaluation.UpgradedUntouchedItems,
			classification.RetryPaths,
			classification.RetryRenames);
	}

	/// <summary>
	/// 入力スナップショットから DB 検索およびディスク走査用の対象パス集合を構築します。
	/// </summary>
	/// <param name="input">差分計算への入力スナップショット</param>
	/// <returns>構築された検索コンテキスト</returns>
	private ReconcileSearchContext BuildSearchContext(ReconcileInput input) {
		var candidateOldPaths = new HashSet<string>(input.DirtyPaths, StringComparer.OrdinalIgnoreCase);
		var candidateNewPaths = new HashSet<string>(input.DirtyPaths, StringComparer.OrdinalIgnoreCase);
		var existingByOldPath = new Dictionary<string, FileChangeItem>(StringComparer.OrdinalIgnoreCase);

		foreach (var existing in input.ScopedExistingItems) {
			if (!string.IsNullOrEmpty(existing.OldPath)) {
				var normOld = FileChangePathUtility.NormalizePath(existing.OldPath);
				candidateOldPaths.Add(normOld);
				if (existing.ChangeType is FileChangeType.Renamed or FileChangeType.Moved) {
					existingByOldPath[normOld] = existing;
				}
			}
			if (!string.IsNullOrEmpty(existing.NewPath)) {
				var normNew = FileChangePathUtility.NormalizePath(existing.NewPath);
				candidateOldPaths.Add(normNew);
				candidateNewPaths.Add(normNew);
			}
		}

		foreach (var op in input.RenameHistory) {
			candidateOldPaths.Add(op.OldPath);
			candidateNewPaths.Add(op.NewPath);
		}

		var untouchedMediaItemIds = input.UntouchedExistingItems
			.Where(x => x.MediaItemId.HasValue)
			.Select(x => x.MediaItemId!.Value)
			.ToHashSet();

		var untouchedNewPaths = input.UntouchedExistingItems
			.Where(x => !string.IsNullOrEmpty(x.NewPath))
			.Select(x => FileChangePathUtility.NormalizePath(x.NewPath))
			.ToHashSet(StringComparer.OrdinalIgnoreCase);

		return new ReconcileSearchContext(
			candidateOldPaths,
			candidateNewPaths,
			existingByOldPath,
			untouchedMediaItemIds,
			untouchedNewPaths);
	}

	/// <summary>
	/// 影響範囲の DB レコードに対してリネーム履歴をリプレイし、期待パスを算出します。
	/// </summary>
	/// <param name="dbItemsById">DBレコード辞書</param>
	/// <param name="context">検索コンテキスト</param>
	/// <param name="renameHistory">時系列のリネーム履歴</param>
	/// <returns>メディアアイテムIDごとの解決済みパス辞書</returns>
	private static Dictionary<long, string> BuildResolvedTargetPaths(
		Dictionary<long, MediaItem> dbItemsById,
		ReconcileSearchContext context,
		List<RenameOperation> renameHistory) {
		var resolvedPathById = new Dictionary<long, string>();
		foreach (var dbItem in dbItemsById.Values) {
			if (context.UntouchedMediaItemIds.Contains(dbItem.MediaItemId)) {
				continue;
			}
			var resolvedPath = ReplayRenames(dbItem.FilePath, context.ExistingByOldPath, renameHistory);
			resolvedPathById[dbItem.MediaItemId] = resolvedPath;
			context.CandidateNewPaths.Add(FileChangePathUtility.NormalizePath(dbItem.FilePath));
			context.CandidateNewPaths.Add(resolvedPath);
		}
		return resolvedPathById;
	}

	/// <summary>
	/// 影響を受ける DB レコードを完全一致およびフォルダプレフィックス検索で取得します。
	/// </summary>
	/// <param name="db">DBコンテキスト</param>
	/// <param name="candidateOldPaths">検索対象の旧パス集合</param>
	/// <param name="cancellationToken">キャンセルトークン</param>
	/// <returns>取得したメディアアイテム辞書</returns>
	private async Task<Dictionary<long, MediaItem>> FetchAffectedDbItemsAsync(
		MediaDeckDbContext db,
		HashSet<string> candidateOldPaths,
		CancellationToken cancellationToken) {
		var result = new Dictionary<long, MediaItem>();
		if (candidateOldPaths.Count == 0) {
			return result;
		}

		var exactMatchedPaths = await this.FetchDbItemsByExactPathAsync(db, candidateOldPaths, result, cancellationToken).ConfigureAwait(false);
		var folderPrefixes = CollectFolderPrefixes(candidateOldPaths, exactMatchedPaths);
		if (folderPrefixes.Count == 0) {
			return result;
		}

		await this.FetchDbItemsByFolderPrefixesAsync(db, folderPrefixes, result, cancellationToken).ConfigureAwait(false);
		return result;
	}

	/// <summary>
	/// パスの完全一致によって DB レコードを検索します。
	/// </summary>
	/// <param name="db">DBコンテキスト</param>
	/// <param name="candidatePaths">検索対象パス集合</param>
	/// <param name="result">検索結果を格納する辞書</param>
	/// <param name="cancellationToken">キャンセルトークン</param>
	/// <returns>通常ファイルとして完全一致したパス集合</returns>
	private async Task<HashSet<string>> FetchDbItemsByExactPathAsync(
		MediaDeckDbContext db,
		HashSet<string> candidatePaths,
		Dictionary<long, MediaItem> result,
		CancellationToken cancellationToken) {
		var exactMatchedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var chunk in candidatePaths.Chunk(500)) {
			var chunkList = chunk.ToList();
			var matches = await db.MediaItems
				.AsNoTracking()
				.Where(m => chunkList.Contains(m.FilePath))
				.ToListAsync(cancellationToken)
				.ConfigureAwait(false);

			foreach (var item in matches) {
				result[item.MediaItemId] = item;
				if (item.MediaType != MediaType.FolderGroup) {
					exactMatchedPaths.Add(FileChangePathUtility.NormalizePath(item.FilePath));
				}
			}
		}
		return exactMatchedPaths;
	}

	/// <summary>
	/// フォルダ検索が必要となる冗長性のないプレフィックス一覧を抽出します。
	/// </summary>
	/// <param name="candidateOldPaths">候補パス集合</param>
	/// <param name="exactMatchedPaths">通常ファイルとして既に一致済みのパス集合</param>
	/// <returns>絞り込まれたフォルダプレフィックス一覧</returns>
	private static List<string> CollectFolderPrefixes(HashSet<string> candidateOldPaths, HashSet<string> exactMatchedPaths) {
		// 通常のファイルとして一致しなかったパス（＝フォルダ削除・移動候補）および FolderGroup パスをプレフィックス検索対象とする
		var rawPrefixes = candidateOldPaths
			.Where(p => !exactMatchedPaths.Contains(p) && !File.Exists(p))
			.OrderBy(p => p.Length)
			.ToList();

		var folderPrefixes = new List<string>();
		foreach (var candidate in rawPrefixes) {
			if (!folderPrefixes.Any(existing => FileChangePathUtility.IsSelfOrDescendant(candidate, existing))) {
				folderPrefixes.Add(candidate);
			}
		}
		return folderPrefixes;
	}

	/// <summary>
	/// フォルダプレフィックス配下の DB レコードを取得して結果辞書へ追加します。
	/// </summary>
	/// <param name="db">DBコンテキスト</param>
	/// <param name="folderPrefixes">検索対象のフォルダプレフィックス一覧</param>
	/// <param name="result">検索結果を格納する辞書</param>
	/// <param name="cancellationToken">キャンセルトークン</param>
	private async Task FetchDbItemsByFolderPrefixesAsync(
		MediaDeckDbContext db,
		List<string> folderPrefixes,
		Dictionary<long, MediaItem> result,
		CancellationToken cancellationToken) {
		if (folderPrefixes.Count <= 5) {
			// 少数のプレフィックスは DB 側で絞り込み、非対象ファイルの単発削除等で全件パスをメモリへ展開しないようにする
			foreach (var prefix in folderPrefixes) {
				var (sepBack, sepFwd) = FileChangePathUtility.CreatePrefixPatterns(prefix);
				var underFolder = await db.MediaItems
					.AsNoTracking()
					.Where(m => m.DirectoryPath == prefix
						|| m.DirectoryPath.StartsWith(sepBack)
						|| m.DirectoryPath.StartsWith(sepFwd)
						|| m.FilePath.StartsWith(sepBack)
						|| m.FilePath.StartsWith(sepFwd))
					.ToListAsync(cancellationToken)
					.ConfigureAwait(false);

				foreach (var item in underFolder) {
					result.TryAdd(item.MediaItemId, item);
				}
			}
			return;
		}

		// 多数プレフィックス時は軽量射影の1回スキャンでまとめて判定し、N回テーブルスキャンを回避する
		var allPathRows = await db.MediaItems
			.AsNoTracking()
			.Select(m => new { m.MediaItemId, m.DirectoryPath, m.FilePath })
			.ToListAsync(cancellationToken)
			.ConfigureAwait(false);

		var matchedIds = allPathRows
			.Where(m => !result.ContainsKey(m.MediaItemId)
				&& folderPrefixes.Any(prefix =>
					FileChangePathUtility.IsSelfOrDescendant(m.DirectoryPath, prefix)
					|| FileChangePathUtility.IsSelfOrDescendant(m.FilePath, prefix)))
			.Select(m => m.MediaItemId)
			.ToList();

		foreach (var chunk in matchedIds.Chunk(500)) {
			var chunkList = chunk.ToList();
			var loaded = await db.MediaItems
				.AsNoTracking()
				.Where(m => chunkList.Contains(m.MediaItemId))
				.ToListAsync(cancellationToken)
				.ConfigureAwait(false);

			foreach (var item in loaded) {
				result[item.MediaItemId] = item;
			}
		}
	}

	/// <summary>
	/// 候補パスのディスク上実態を走査し、実在ファイルと <see cref="MediaType.FolderGroup"/> 候補ディレクトリを収集します。
	/// </summary>
	/// <param name="candidateNewPaths">走査対象の候補パス集合</param>
	/// <param name="untouchedNewPaths">除外すべき未変更対象パス集合</param>
	/// <param name="diskFiles">収集した実在ファイル集合</param>
	/// <param name="diskFolderGroups">収集したフォルダグループ候補ディレクトリ集合</param>
	private void CollectDiskEntries(
		HashSet<string> candidateNewPaths,
		HashSet<string> untouchedNewPaths,
		HashSet<string> diskFiles,
		HashSet<string> diskFolderGroups) {
		var enumFileOptions = new EnumerationOptions {
			IgnoreInaccessible = true,
			RecurseSubdirectories = true
		};
		var enumDirOptions = new EnumerationOptions {
			IgnoreInaccessible = true,
			RecurseSubdirectories = false
		};

		foreach (var path in candidateNewPaths) {
			if (File.Exists(path)) {
				diskFiles.Add(FileChangePathUtility.NormalizePath(path));
				continue;
			}
			if (!Directory.Exists(path)) {
				continue;
			}

			try {
				foreach (var file in Directory.EnumerateFiles(path, "*", enumFileOptions)) {
					diskFiles.Add(FileChangePathUtility.NormalizePath(file));
				}

				if (this.IsGroupingRoot(path)) {
					foreach (var subDir in Directory.EnumerateDirectories(path, "*", enumDirOptions)) {
						diskFolderGroups.Add(FileChangePathUtility.NormalizePath(subDir));
					}
				} else if (this.IsGroupingRootChildDirectory(path)) {
					diskFolderGroups.Add(FileChangePathUtility.NormalizePath(path));
				}
			} catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
				this._logger.LogDebug(ex, "Failed to enumerate directory during change reconciliation: {Path}", path);
			}
		}

		foreach (var untouchedPath in untouchedNewPaths) {
			diskFiles.Remove(untouchedPath);
			diskFolderGroups.Remove(untouchedPath);
		}
	}

	/// <summary>
	/// 影響範囲の DB レコードを「変更なし」「リネーム／移動」「消失」へ分類し、ディスク上の実在パスを消費します。
	/// </summary>
	/// <param name="dbItemsById">DBレコード辞書</param>
	/// <param name="resolvedPathById">解決済みパス辞書</param>
	/// <param name="context">検索コンテキスト</param>
	/// <param name="diskFiles">実在ファイル集合（消費されたものは除去）</param>
	/// <param name="diskFolderGroups">実在フォルダグループ候補集合（消費されたものは除去）</param>
	/// <returns>DBレコード分類結果</returns>
	private DbClassificationResult ClassifyDbItems(
		Dictionary<long, MediaItem> dbItemsById,
		Dictionary<long, string> resolvedPathById,
		ReconcileSearchContext context,
		HashSet<string> diskFiles,
		HashSet<string> diskFolderGroups) {
		var evaluatedDbPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var pathChangedItems = new List<(MediaItem DbItem, string DbNormPath, string ResolvedPath)>();
		var pathUnchangedItems = new List<(MediaItem DbItem, string DbNormPath)>();

		foreach (var dbItem in dbItemsById.Values) {
			if (context.UntouchedMediaItemIds.Contains(dbItem.MediaItemId)) {
				continue;
			}
			var dbNormPath = FileChangePathUtility.NormalizePath(dbItem.FilePath);
			evaluatedDbPaths.Add(dbNormPath);
			var resolvedPath = resolvedPathById[dbItem.MediaItemId];
			if (!string.Equals(resolvedPath, dbNormPath, StringComparison.Ordinal)) {
				pathChangedItems.Add((dbItem, dbNormPath, resolvedPath));
			} else {
				pathUnchangedItems.Add((dbItem, dbNormPath));
			}
		}

		var reconciledItems = new List<FileChangeItem>();
		var missingDbItems = new List<MediaItem>();
		var retryPaths = new List<string>();
		var retryRenames = new List<RenameOperation>();
		var claimedTargetPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		// パス変更（Renamed/Moved）を先に確定して遷移先を消費し、その後に元パスの残存有無を判定する
		this.ClassifyPathChangedItems(
			pathChangedItems,
			context.ExistingByOldPath,
			diskFiles,
			diskFolderGroups,
			claimedTargetPaths,
			reconciledItems,
			missingDbItems,
			retryPaths,
			retryRenames);

		this.ClassifyPathUnchangedItems(
			pathUnchangedItems,
			claimedTargetPaths,
			diskFiles,
			diskFolderGroups,
			missingDbItems);

		return new DbClassificationResult(reconciledItems, missingDbItems, retryPaths, retryRenames, evaluatedDbPaths);
	}

	/// <summary>
	/// リネーム履歴によりパスが変化した DB レコードを評価します。
	/// </summary>
	private void ClassifyPathChangedItems(
		List<(MediaItem DbItem, string DbNormPath, string ResolvedPath)> pathChangedItems,
		Dictionary<string, FileChangeItem> existingByOldPath,
		HashSet<string> diskFiles,
		HashSet<string> diskFolderGroups,
		HashSet<string> claimedTargetPaths,
		List<FileChangeItem> reconciledItems,
		List<MediaItem> missingDbItems,
		List<string> retryPaths,
		List<RenameOperation> retryRenames) {
		foreach (var (dbItem, dbNormPath, resolvedPath) in pathChangedItems) {
			var isDirectory = dbItem.MediaType == MediaType.FolderGroup;
			long targetFileSize = 0;
			var existsAtTarget = false;
			var ioBlocked = false;

			if (isDirectory) {
				existsAtTarget = Directory.Exists(resolvedPath);
			} else if (this.IsTargetFile(resolvedPath)) {
				try {
					var fi = new FileInfo(resolvedPath);
					if (fi.Exists) {
						targetFileSize = fi.Length;
						existsAtTarget = true;
						this._resetRetryCount(resolvedPath);
					}
				} catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
					if (this._tryConsumeRetrySlot(resolvedPath)) {
						ioBlocked = true;
					} else {
						this._logger.LogWarning(ex, "Exceeded retry limit while reading renamed file info; falling back to DB size: {Path}", resolvedPath);
						targetFileSize = dbItem.FileSize;
						existsAtTarget = File.Exists(resolvedPath);
					}
				}
			}

			if (ioBlocked) {
				retryPaths.Add(dbNormPath);
				retryPaths.Add(resolvedPath);
				retryRenames.Add(new RenameOperation(dbNormPath, resolvedPath));
				continue;
			}

			if (!existsAtTarget) {
				missingDbItems.Add(dbItem);
				continue;
			}

			var wasMoved = existingByOldPath.TryGetValue(dbNormPath, out var prev) && prev.ChangeType == FileChangeType.Moved;
			reconciledItems.Add(new FileChangeItem {
				MediaItemId = dbItem.MediaItemId,
				ChangeType = wasMoved ? FileChangeType.Moved : FileChangeType.Renamed,
				OldPath = dbItem.FilePath,
				NewPath = resolvedPath,
				FileSize = targetFileSize,
				OldHash = dbItem.PreHash ?? string.Empty,
				IsPending = false
			});

			claimedTargetPaths.Add(resolvedPath);
			if (isDirectory) {
				diskFolderGroups.Remove(resolvedPath);
			} else {
				diskFiles.Remove(resolvedPath);
			}
		}
	}

	/// <summary>
	/// 元のパスから変化していない DB レコードの残存有無を判定します。
	/// </summary>
	private void ClassifyPathUnchangedItems(
		List<(MediaItem DbItem, string DbNormPath)> pathUnchangedItems,
		HashSet<string> claimedTargetPaths,
		HashSet<string> diskFiles,
		HashSet<string> diskFolderGroups,
		List<MediaItem> missingDbItems) {
		foreach (var (dbItem, dbNormPath) in pathUnchangedItems) {
			var isDirectory = dbItem.MediaType == MediaType.FolderGroup;
			var existsAtOriginal = !claimedTargetPaths.Contains(dbNormPath) && (isDirectory
				? Directory.Exists(dbItem.FilePath)
				: (File.Exists(dbItem.FilePath) && this.IsTargetFile(dbItem.FilePath)));

			if (!existsAtOriginal) {
				missingDbItems.Add(dbItem);
				continue;
			}

			// 変更なし（同一パス上の再作成・上書き保存を含む）
			if (isDirectory) {
				diskFolderGroups.Remove(dbNormPath);
			} else {
				diskFiles.Remove(dbNormPath);
			}
		}
	}

	/// <summary>
	/// 未消費のディスクアイテムから対象外拡張子・既存DB登録済み・同一パス上書き保存（Atomic Save）を除外します。
	/// </summary>
	/// <param name="db">DBコンテキスト</param>
	/// <param name="diskFiles">未消費の実在ファイル集合</param>
	/// <param name="diskFolderGroups">未消費のフォルダグループ候補集合</param>
	/// <param name="evaluatedDbPaths">既に評価済みの DB パス集合</param>
	/// <param name="missingDbItems">消失した DB レコード一覧（同一パス上書き分を除去）</param>
	/// <param name="cancellationToken">キャンセルトークン</param>
	/// <returns>新規または移動先候補となる実在ファイルパス一覧</returns>
	private async Task<List<string>> ExcludeRegisteredAndOverwrittenFilesAsync(
		MediaDeckDbContext db,
		HashSet<string> diskFiles,
		HashSet<string> diskFolderGroups,
		HashSet<string> evaluatedDbPaths,
		List<MediaItem> missingDbItems,
		CancellationToken cancellationToken) {
		var targetDiskFiles = diskFiles.Where(this.IsTargetFile).ToList();
		var uncheckedDiskPaths = targetDiskFiles
			.Concat(diskFolderGroups)
			.Where(p => !evaluatedDbPaths.Contains(p))
			.ToList();

		if (uncheckedDiskPaths.Count > 0) {
			var alreadyRegisteredPaths = await FetchExistingDbPathsAsync(db, uncheckedDiskPaths, cancellationToken).ConfigureAwait(false);
			targetDiskFiles.RemoveAll(p => alreadyRegisteredPaths.Contains(p));
			diskFolderGroups.RemoveWhere(p => alreadyRegisteredPaths.Contains(p));
		}

		// 同一パスでの削除+再作成（Atomic Saveの別パターン）を相殺
		var remainingDiskFiles = new List<string>(targetDiskFiles.Count);
		foreach (var diskFile in targetDiskFiles) {
			var samePathMissingIndex = missingDbItems.FindIndex(m =>
				m.MediaType != MediaType.FolderGroup &&
				string.Equals(FileChangePathUtility.NormalizePath(m.FilePath), diskFile, StringComparison.OrdinalIgnoreCase));

			if (samePathMissingIndex >= 0) {
				missingDbItems.RemoveAt(samePathMissingIndex);
			} else {
				remainingDiskFiles.Add(diskFile);
			}
		}

		return remainingDiskFiles;
	}

	/// <summary>
	/// 未登録ファイルのハッシュを計算し、消失レコードとの <see cref="FileChangeType.Moved"/> ペアリングおよび <see cref="FileChangeType.Added"/> 生成を行います。
	/// </summary>
	private AddedEvaluationResult EvaluateAddedAndCrossBatchMoves(
		List<string> remainingDiskFiles,
		HashSet<string> diskFolderGroups,
		List<MediaItem> missingDbItems,
		List<FileChangeItem> untouchedExistingItems,
		List<FileChangeItem> reconciledItems,
		List<string> retryPaths,
		CancellationToken cancellationToken) {
		var upgradedUntouchedItems = new List<(FileChangeItem OldItem, FileChangeItem NewItem)>();
		var matchedUntouchedItems = new HashSet<FileChangeItem>();
		var newAddedItems = new List<FileChangeItem>();

		foreach (var newFilePath in remainingDiskFiles) {
			cancellationToken.ThrowIfCancellationRequested();

			if (!this.TryReadFileSizeAndHash(newFilePath, out var fileSize, out var newHash, out var retryNeeded)) {
				if (retryNeeded) {
					retryPaths.Add(newFilePath);
					continue;
				}
				if (fileSize >= 0 && string.IsNullOrEmpty(newHash)) {
					// リトライ上限到達または読み取りフォールバック時はハッシュなしで Added とする
					newAddedItems.Add(new FileChangeItem {
						MediaItemId = null,
						ChangeType = FileChangeType.Added,
						OldPath = string.Empty,
						NewPath = newFilePath,
						FileSize = fileSize,
						NewHash = string.Empty,
						IsPending = false
					});
				}
				continue;
			}

			var matchedMissing = FindBestHashMatch(missingDbItems, newHash, fileSize, newFilePath);
			if (matchedMissing != null) {
				missingDbItems.Remove(matchedMissing);
				reconciledItems.Add(new FileChangeItem {
					MediaItemId = matchedMissing.MediaItemId,
					ChangeType = FileChangeType.Moved,
					OldPath = matchedMissing.FilePath,
					NewPath = newFilePath,
					FileSize = fileSize,
					OldHash = matchedMissing.PreHash ?? string.Empty,
					NewHash = newHash,
					IsPending = false
				});
				continue;
			}

			var matchedUntouchedDeleted = FindBestUntouchedDeletedMatch(untouchedExistingItems, matchedUntouchedItems, newHash, fileSize, newFilePath);
			if (matchedUntouchedDeleted != null) {
				matchedUntouchedItems.Add(matchedUntouchedDeleted);
				upgradedUntouchedItems.Add((matchedUntouchedDeleted, new FileChangeItem {
					MediaItemId = matchedUntouchedDeleted.MediaItemId,
					ChangeType = FileChangeType.Moved,
					OldPath = matchedUntouchedDeleted.OldPath,
					NewPath = newFilePath,
					FileSize = fileSize,
					OldHash = matchedUntouchedDeleted.OldHash,
					NewHash = newHash,
					IsPending = false
				}));
				continue;
			}

			newAddedItems.Add(new FileChangeItem {
				MediaItemId = null,
				ChangeType = FileChangeType.Added,
				OldPath = string.Empty,
				NewPath = newFilePath,
				FileSize = fileSize,
				NewHash = newHash,
				IsPending = false
			});
		}

		reconciledItems.AddRange(newAddedItems);

		foreach (var newDirPath in diskFolderGroups) {
			reconciledItems.Add(new FileChangeItem {
				MediaItemId = null,
				ChangeType = FileChangeType.Added,
				OldPath = string.Empty,
				NewPath = newDirPath,
				FileSize = 0,
				IsPending = false
			});
		}

		return new AddedEvaluationResult(upgradedUntouchedItems, matchedUntouchedItems);
	}

	/// <summary>
	/// 新規候補ファイルのサイズとハッシュを安全に読み取ります。ロック中はリトライ可否を返します。
	/// </summary>
	/// <param name="filePath">対象ファイルパス</param>
	/// <param name="fileSize">取得したファイルサイズ（読み取り不可でスキップする場合は -1）</param>
	/// <param name="fileHash">計算したハッシュ値</param>
	/// <param name="retryNeeded">次回リトライが必要な場合は true</param>
	/// <returns>サイズとハッシュの両方を取得できた場合は true</returns>
	private bool TryReadFileSizeAndHash(string filePath, out long fileSize, out string fileHash, out bool retryNeeded) {
		fileSize = -1;
		fileHash = string.Empty;
		retryNeeded = false;

		try {
			var fileInfo = new FileInfo(filePath);
			if (!fileInfo.Exists) {
				return false;
			}
			fileSize = fileInfo.Length;
			fileHash = FileHashUtility.ComputeFileHash(filePath);
			this._resetRetryCount(filePath);
			return true;
		} catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
			if (this._tryConsumeRetrySlot(filePath)) {
				retryNeeded = true;
				return false;
			}

			this._logger.LogWarning(ex, "Exceeded retry limit while reading file hash; adding without hash: {Path}", filePath);
			fileSize = 0;
			try {
				var fi = new FileInfo(filePath);
				if (fi.Exists) {
					fileSize = fi.Length;
				}
			} catch {
				// サイズ取得失敗時は0のまま扱う
			}
			return false;
		} catch (Exception ex) {
			this._logger.LogError(ex, "Failed to inspect added file: {Path}", filePath);
			return false;
		}
	}

	/// <summary>
	/// 残存した消失 DB レコードを既存 <see cref="FileChangeType.Added"/> とのクロスバッチ <see cref="FileChangeType.Moved"/> または <see cref="FileChangeType.Deleted"/> として確定します。
	/// </summary>
	private static void EvaluateRemainingMissingItems(
		List<MediaItem> missingDbItems,
		List<FileChangeItem> untouchedExistingItems,
		HashSet<FileChangeItem> matchedUntouchedItems,
		List<(FileChangeItem OldItem, FileChangeItem NewItem)> upgradedUntouchedItems,
		List<FileChangeItem> reconciledItems) {
		foreach (var missing in missingDbItems) {
			var matchedUntouchedAdded = missing.MediaType != MediaType.FolderGroup && !string.IsNullOrEmpty(missing.PreHash)
				? FindBestUntouchedAddedMatch(untouchedExistingItems, matchedUntouchedItems, missing.PreHash, missing.FileSize, missing.FilePath)
				: null;

			if (matchedUntouchedAdded != null) {
				matchedUntouchedItems.Add(matchedUntouchedAdded);
				upgradedUntouchedItems.Add((matchedUntouchedAdded, new FileChangeItem {
					MediaItemId = missing.MediaItemId,
					ChangeType = FileChangeType.Moved,
					OldPath = missing.FilePath,
					NewPath = matchedUntouchedAdded.NewPath,
					FileSize = matchedUntouchedAdded.FileSize,
					OldHash = missing.PreHash ?? string.Empty,
					NewHash = matchedUntouchedAdded.NewHash,
					IsPending = false
				}));
				continue;
			}

			reconciledItems.Add(new FileChangeItem {
				MediaItemId = missing.MediaItemId,
				ChangeType = FileChangeType.Deleted,
				OldPath = missing.FilePath,
				NewPath = string.Empty,
				FileSize = missing.FileSize,
				OldHash = missing.PreHash ?? string.Empty,
				IsPending = false
			});
		}
	}

	/// <summary>
	/// 候補パスのうち既に DB に登録されているパス集合を取得します。
	/// </summary>
	private static async Task<HashSet<string>> FetchExistingDbPathsAsync(
		MediaDeckDbContext db,
		List<string> paths,
		CancellationToken cancellationToken) {
		var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var chunk in paths.Chunk(500)) {
			var chunkList = chunk.ToList();
			var existing = await db.MediaItems
				.AsNoTracking()
				.Where(m => chunkList.Contains(m.FilePath))
				.Select(m => m.FilePath)
				.ToListAsync(cancellationToken)
				.ConfigureAwait(false);
			foreach (var path in existing) {
				result.Add(FileChangePathUtility.NormalizePath(path));
			}
		}
		return result;
	}

	/// <summary>
	/// 指定されたファイルパスが設定された対象拡張子に一致するかを判定します。
	/// </summary>
	private bool IsTargetFile(string filePath) {
		var extension = Path.GetExtension(filePath);
		if (string.IsNullOrWhiteSpace(extension)) {
			return false;
		}

		return this._config.ScanConfig.TargetExtensions
			.Any(x => string.Equals(x.Extension.Value, extension, StringComparison.OrdinalIgnoreCase));
	}

	/// <summary>
	/// 指定されたディレクトリがフォルダグループ化のルート監視フォルダであるかを判定します。
	/// </summary>
	private bool IsGroupingRoot(string dirPath) {
		var normDir = FileChangePathUtility.NormalizePath(dirPath);
		return this._config.FolderManagerConfig.Folders
			.Any(f => f.IsGroupingRoot && string.Equals(FileChangePathUtility.NormalizePath(f.FolderPath), normDir, StringComparison.OrdinalIgnoreCase));
	}

	/// <summary>
	/// 指定されたディレクトリがグループ化ルート監視フォルダの直下ディレクトリであるかを判定します。
	/// </summary>
	private bool IsGroupingRootChildDirectory(string dirPath) {
		var normDir = FileChangePathUtility.NormalizePath(dirPath);
		var parentDir = Path.GetDirectoryName(normDir);
		if (string.IsNullOrEmpty(parentDir)) {
			return false;
		}

		var normParent = FileChangePathUtility.NormalizePath(parentDir);
		return this._config.FolderManagerConfig.Folders
			.Any(f => f.IsGroupingRoot && string.Equals(FileChangePathUtility.NormalizePath(f.FolderPath), normParent, StringComparison.OrdinalIgnoreCase));
	}

	/// <summary>
	/// DB上のパスに対し、既存の未処理リネームおよび今回のリネーム履歴を時系列にリプレイして移動先パスを算出します。
	/// </summary>
	private static string ReplayRenames(
		string originalDbPath,
		Dictionary<string, FileChangeItem> existingByOldPath,
		List<RenameOperation> renameHistory) {
		var currentPath = FileChangePathUtility.NormalizePath(originalDbPath);
		if (existingByOldPath.TryGetValue(currentPath, out var seeded) && !string.IsNullOrEmpty(seeded.NewPath)) {
			currentPath = FileChangePathUtility.NormalizePath(seeded.NewPath);
		}

		foreach (var op in renameHistory) {
			if (string.Equals(currentPath, op.OldPath, StringComparison.OrdinalIgnoreCase)) {
				currentPath = op.NewPath;
			} else if (FileChangePathUtility.IsSubPathOf(currentPath, op.OldPath)) {
				currentPath = FileChangePathUtility.ReplacePathPrefix(currentPath, op.OldPath, op.NewPath);
			}
		}

		return currentPath;
	}

	/// <summary>
	/// ハッシュ値・ファイルサイズ・ファイル名に基づいて最適な消失 DB レコード候補を検索します。
	/// </summary>
	private static MediaItem? FindBestHashMatch(List<MediaItem> candidates, string hash, long fileSize, string targetPath) {
		if (string.IsNullOrEmpty(hash)) {
			return null;
		}

		var fileName = Path.GetFileName(targetPath);
		return candidates
			.Where(m => m.MediaType != MediaType.FolderGroup && !string.IsNullOrEmpty(m.PreHash) && string.Equals(m.PreHash, hash, StringComparison.OrdinalIgnoreCase))
			.OrderByDescending(m => m.FileSize == fileSize)
			.ThenByDescending(m => string.Equals(Path.GetFileName(m.FilePath), fileName, StringComparison.OrdinalIgnoreCase))
			.FirstOrDefault();
	}

	/// <summary>
	/// 既存の未処理削除アイテムから同一ハッシュの移動元候補を検索します。
	/// </summary>
	private static FileChangeItem? FindBestUntouchedDeletedMatch(
		List<FileChangeItem> untouchedItems,
		HashSet<FileChangeItem> alreadyMatched,
		string hash,
		long fileSize,
		string targetPath) {
		if (string.IsNullOrEmpty(hash)) {
			return null;
		}

		var fileName = Path.GetFileName(targetPath);
		return untouchedItems
			.Where(x => !alreadyMatched.Contains(x)
				&& x.ChangeType == FileChangeType.Deleted
				&& x.MediaItemId.HasValue
				&& !string.IsNullOrEmpty(x.OldHash)
				&& string.Equals(x.OldHash, hash, StringComparison.OrdinalIgnoreCase))
			.OrderByDescending(x => x.FileSize == fileSize)
			.ThenByDescending(x => string.Equals(Path.GetFileName(x.OldPath), fileName, StringComparison.OrdinalIgnoreCase))
			.FirstOrDefault();
	}

	/// <summary>
	/// 既存の未処理追加アイテムから同一ハッシュの移動先候補を検索します。
	/// </summary>
	private static FileChangeItem? FindBestUntouchedAddedMatch(
		List<FileChangeItem> untouchedItems,
		HashSet<FileChangeItem> alreadyMatched,
		string hash,
		long fileSize,
		string oldPath) {
		if (string.IsNullOrEmpty(hash)) {
			return null;
		}

		var fileName = Path.GetFileName(oldPath);
		return untouchedItems
			.Where(x => !alreadyMatched.Contains(x)
				&& x.ChangeType == FileChangeType.Added
				&& !x.MediaItemId.HasValue
				&& !string.IsNullOrEmpty(x.NewHash)
				&& string.Equals(x.NewHash, hash, StringComparison.OrdinalIgnoreCase))
			.OrderByDescending(x => x.FileSize == fileSize)
			.ThenByDescending(x => string.Equals(Path.GetFileName(x.NewPath), fileName, StringComparison.OrdinalIgnoreCase))
			.FirstOrDefault();
	}

	/// <summary>
	/// 差分計算に使用する検索対象パスおよび維持アイテム情報をまとめたコンテキストです。
	/// </summary>
	/// <param name="CandidateOldPaths">DB検索候補となる旧パス集合</param>
	/// <param name="CandidateNewPaths">ディスク走査候補となる新パス集合</param>
	/// <param name="ExistingByOldPath">既存の移動・リネーム未処理アイテム辞書</param>
	/// <param name="UntouchedMediaItemIds">今回の評価対象外として維持するメディアアイテムID集合</param>
	/// <param name="UntouchedNewPaths">今回の評価対象外として除外する新パス集合</param>
	private sealed record ReconcileSearchContext(
		HashSet<string> CandidateOldPaths,
		HashSet<string> CandidateNewPaths,
		Dictionary<string, FileChangeItem> ExistingByOldPath,
		HashSet<long> UntouchedMediaItemIds,
		HashSet<string> UntouchedNewPaths);

	/// <summary>
	/// 影響範囲の DB レコードに対する分類結果を保持します。
	/// </summary>
	/// <param name="ReconciledItems">確定した変更アイテム一覧</param>
	/// <param name="MissingDbItems">ディスク上で見つからなかった消失 DB レコード一覧</param>
	/// <param name="RetryPaths">I/O ロック等で再試行が必要なパス一覧</param>
	/// <param name="RetryRenames">再試行時に引き継ぐリネーム操作一覧</param>
	/// <param name="EvaluatedDbPaths">既に評価済みの DB 正規化パス集合</param>
	private sealed record DbClassificationResult(
		List<FileChangeItem> ReconciledItems,
		List<MediaItem> MissingDbItems,
		List<string> RetryPaths,
		List<RenameOperation> RetryRenames,
		HashSet<string> EvaluatedDbPaths);

	/// <summary>
	/// 未登録ファイルとクロスバッチ移動の評価結果を保持します。
	/// </summary>
	/// <param name="UpgradedUntouchedItems">Moved へ昇格した既存アイテムの置換ペア一覧</param>
	/// <param name="MatchedUntouchedItems">既にペアリング消費された既存未処理アイテム集合</param>
	private sealed record AddedEvaluationResult(
		List<(FileChangeItem OldItem, FileChangeItem NewItem)> UpgradedUntouchedItems,
		HashSet<FileChangeItem> MatchedUntouchedItems);
}