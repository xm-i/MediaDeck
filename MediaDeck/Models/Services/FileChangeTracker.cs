using System.IO;
using System.Threading.Tasks;

using MediaDeck.Common.Base;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Database;

using Microsoft.Extensions.Logging;

namespace MediaDeck.Models.Services;

/// <summary>
/// 未処理のファイル変更リストの管理・マッチング・統合ロジックを担うクラスです。
/// </summary>
[Inject(InjectServiceLifetime.Singleton)]
public class FileChangeTracker : IDisposable {
	private readonly IDbContextFactory<MediaDeckDbContext> _dbFactory;
	private readonly ILogger<FileChangeTracker> _logger;
	private readonly CompositeDisposable _compositeDisposable = new();

	/// <summary>
	/// 未処理の変更リスト
	/// </summary>
	public ObservableList<FileChangeItem> UnprocessedChanges { get; } = new();

	/// <summary>
	/// FileChangeTrackerクラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="dbFactory">DBコンテキストファクトリー</param>
	/// <param name="logger">ロガー</param>
	public FileChangeTracker(IDbContextFactory<MediaDeckDbContext> dbFactory, ILogger<FileChangeTracker> logger) {
		this._dbFactory = dbFactory;
		this._logger = logger;

		this.UnprocessedChanges
			.ObserveAdd()
			.SubscribeAwait(async (_, ct) => {
				await this.ProcessPendingChangesAsync();
			}, AwaitOperation.ThrottleFirstLast)
			.AddTo(this._compositeDisposable);
	}

	/// <summary>
	/// ファイル作成イベントを通知します。
	/// </summary>
	/// <param name="path">作成されたファイルのパス</param>
	public void OnCreated(string path) {
		this._logger.LogDebug("File created (Pending): {Path}", path);
		var model = new FileChangeItem {
			ChangeType = FileChangeType.Added,
			NewPath = path,
			IsPending = true
		};
		this.UnprocessedChanges.Add(model);
	}

	/// <summary>
	/// ファイル削除イベントを通知します。
	/// </summary>
	/// <param name="path">削除されたファイルのパス</param>
	public void OnDeleted(string path) {
		this._logger.LogDebug("File deleted (Pending): {Path}", path);
		var model = new FileChangeItem {
			ChangeType = FileChangeType.Deleted,
			OldPath = path,
			IsPending = true
		};
		this.UnprocessedChanges.Add(model);
	}

	/// <summary>
	/// ファイル名変更（同一ドライブ内移動）イベントを通知します。
	/// </summary>
	/// <param name="oldPath">変更前のパス</param>
	/// <param name="newPath">変更後のパス</param>
	public void OnRenamed(string oldPath, string newPath) {
		this._logger.LogDebug("File renamed (Pending): {OldPath} -> {NewPath}", oldPath, newPath);
		var model = new FileChangeItem {
			ChangeType = FileChangeType.Renamed,
			OldPath = oldPath,
			NewPath = newPath,
			IsPending = true
		};
		this.UnprocessedChanges.Add(model);
	}

	/// <summary>
	/// ファイル変更イベントを通知します。
	/// </summary>
	/// <param name="path">変更されたファイルのパス</param>
	public void OnChanged(string path) {
		this._logger.LogDebug("File changed (Pending): {Path}", path);
		// Addedアイテムが既にあれば時間をリセットしてハッシュ計算を遅らせる
		var existing = this.UnprocessedChanges.FirstOrDefault(c => c.ChangeType == FileChangeType.Added && c.NewPath == path);
		existing?.CreatedAt = DateTime.UtcNow;

	}

	/// <summary>
	/// 未確定アイテムの情報を補完し、連続した変更を統合します。
	/// </summary>
	private async Task ProcessPendingChangesAsync() {
		await this.EnrichPendingItemsAsync();
		this.ConsolidateEvents();
	}

	/// <summary>
	/// 未確定アイテム（IsPending = true）に対して、DB検索やFileInfo取得を行い情報を補完します。
	/// </summary>
	private async Task EnrichPendingItemsAsync() {
		// 発生順に処理することで、A->B, B->C のような連鎖で前者の情報を後者が引き継げるようにする
		var pendingItems = this.UnprocessedChanges.Where(c => c.IsPending).ToList();
		if (!pendingItems.Any()) {
			return;
		}

		await using var db = await this._dbFactory.CreateDbContextAsync();
		foreach (var item in pendingItems) {
			if (item.ChangeType == FileChangeType.Deleted || item.ChangeType == FileChangeType.Renamed) {
				// 1. 先行する変更アイテムが既に UnprocessedChanges にないか確認 (A->B, B->C のケース対応)
				var predecessor = this.UnprocessedChanges.FirstOrDefault(c => !c.IsPending && c.NewPath == item.OldPath);
				if (predecessor != null) {
					item.MediaFileId = predecessor.MediaFileId;
					item.FileSize = predecessor.FileSize;
					item.OldHash = predecessor.OldHash;
				} else {
					// 2. DBから取得
					var file = await db.MediaFiles.FirstOrDefaultAsync(mf => mf.FilePath == item.OldPath);
					if (file != null) {
						item.MediaFileId = file.MediaFileId;
						item.FileSize = file.FileSize;
						item.OldHash = file.PreHash ?? string.Empty;
					}
				}
				item.IsPending = false;
			} else if (item.ChangeType == FileChangeType.Added) {
				// 最終更新から2秒経過しているもののみハッシュを計算する (安定待ち)
				if (DateTime.UtcNow - item.CreatedAt < TimeSpan.FromSeconds(2)) {
					break; // 以降のアイテムも時系列順に待機させる
				}

				var fileInfo = new FileInfo(item.NewPath);
				if (!fileInfo.Exists) {
					item.IsPending = false; // ファイルが消えたなら処理終了
					continue;
				}

				try {
					item.FileSize = fileInfo.Length;
					var hash = FileHashUtility.ComputeFileHash(item.NewPath);
					if (string.IsNullOrEmpty(hash)) {
						break; // 再トライのために中断
					}

					item.NewHash = hash;
					item.IsPending = false;
				} catch (IOException) {
					// ファイルロック中などは次のループでリトライのために中断
					break;
				} catch (Exception ex) {
					this._logger.LogError(ex, "Failed to enrich Added file: {Path}", item.NewPath);
					item.IsPending = false;
				}
			}
		}
	}

	/// <summary>
	/// 同一ファイルに対する複数の変更（A->B, B->C, 削除+追加など）を時系列順に統合します。
	/// </summary>
	private void ConsolidateEvents() {
		for (int i = 0; i < this.UnprocessedChanges.Count; i++) {
			var item = this.UnprocessedChanges[i];
			if (item.IsPending) {
				break;
			}

			// 追加以外でMediaFileIdがないアイテムはもとから管理していなかったファイルのため、対象外として削除する。
			if (item.ChangeType != FileChangeType.Added && item.MediaFileId is null) {
				this.UnprocessedChanges.RemoveAt(i);
				i--;
				continue;
			}


			for (int j = i + 1; j < this.UnprocessedChanges.Count; j++) {
				var next = this.UnprocessedChanges[j];
				if (next.IsPending) {
					break;
				}

				if (this.TryMergeItems(item, next)) {
					this.UnprocessedChanges.RemoveAt(j);
					j--; // 次の要素を再チェック

					// 元の位置に戻ったか、不要な変更になった場合は削除して終了
					if (item.OldPath == item.NewPath && item.ChangeType != FileChangeType.Deleted) {
						this.UnprocessedChanges.RemoveAt(i);
						i--;
						break;
					}
					this.UnprocessedChanges[i] = item;
				}
			}
		}
	}

	/// <summary>
	/// 2つのアイテムが同一ファイルの変更であれば、一つに統合します。
	/// </summary>
	private bool TryMergeItems(FileChangeItem item, FileChangeItem next) {
		bool match = false;

		// 1. 同一MediaFileIdによるマッチ
		if (item.MediaFileId != null && item.MediaFileId == next.MediaFileId) {
			match = true;
		}
		// 2. パス鎖によるマッチ (A->B, B->C)
		else if (!string.IsNullOrEmpty(item.NewPath) && item.NewPath == next.OldPath) {
			match = true;
		}
		// 3. ハッシュによる同一性のマッチ (削除+追加のペアによる移動検知)
		else if (item.ChangeType == FileChangeType.Deleted && next.ChangeType == FileChangeType.Added &&
				 !string.IsNullOrEmpty(item.OldHash) && item.OldHash == next.NewHash) {
			match = true;
		}

		if (!match) {
			return false;
		}

		// 状態の統合
		if (item.ChangeType == FileChangeType.Deleted && (next.ChangeType == FileChangeType.Added || next.ChangeType == FileChangeType.Renamed || next.ChangeType == FileChangeType.Moved)) {
			item.ChangeType = FileChangeType.Moved;
		} else {
			item.ChangeType = next.ChangeType;
		}

		item.NewPath = next.NewPath;
		if (!string.IsNullOrEmpty(next.NewHash)) {
			item.NewHash = next.NewHash;
		}
		if (!string.IsNullOrEmpty(next.OldHash) && string.IsNullOrEmpty(item.OldHash)) {
			item.OldHash = next.OldHash;
		}
		if (string.IsNullOrEmpty(item.OldPath) && !string.IsNullOrEmpty(next.OldPath)) {
			item.OldPath = next.OldPath;
		}

		return true;
	}

	/// <summary>
	/// 指定されたアイテムをリストから一括削除します。
	/// </summary>
	/// <param name="items">削除対象のアイテム一覧</param>
	public void RemoveItems(System.Collections.Generic.IEnumerable<FileChangeItem> items) {
		var targetElements = this.UnprocessedChanges
			.Where(x => items.Any(i => i.MediaFileId == x.MediaFileId && (x.MediaFileId != null || i.NewPath == x.NewPath)))
			.ToList();
		this.UnprocessedChanges.RemoveRange(targetElements);
	}

	/// <summary>
	/// リソースを破棄します。
	/// </summary>
	public void Dispose() {
		this._compositeDisposable.Dispose();
	}
}
