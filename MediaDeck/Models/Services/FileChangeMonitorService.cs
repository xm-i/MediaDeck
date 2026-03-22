using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;

using MediaDeck.Composition.Interfaces;
using MediaDeck.Database;
using MediaDeck.Stores.State;
using MediaDeck.Views;

using Microsoft.Extensions.Logging;

namespace MediaDeck.Models.Services;

public enum FileChangeType {
	Deleted,
	Moved
}

public class FileChangeItem {
	public long MediaFileId {
		get; set;
	}
	public string OldPath { get; set; } = string.Empty;
	public string NewPath { get; set; } = string.Empty;
	public FileChangeType ChangeType {
		get; set;
	}

	public string ChangeTypeText {
		get {
			return this.ChangeType == FileChangeType.Deleted ? "削除" : "移動";
		}
	}

	public string PathText {
		get {
			return this.ChangeType == FileChangeType.Deleted ? this.OldPath : $"{this.OldPath} ➔ {this.NewPath}";
		}
	}
}

[Inject(InjectServiceLifetime.Singleton)]
public class FileChangeMonitorService : IDisposable {
	private readonly StateStore _stateStore;
	private readonly IDbContextFactory<MediaDeckDbContext> _dbFactory;
	private readonly ILogger<FileChangeMonitorService> _logger;
	private readonly IDispatcherGate _dispatcherGate;
	private readonly ConcurrentDictionary<string, FileSystemWatcher> _watchers = new();

	public ObservableList<FileChangeItem> UnprocessedChanges { get; } = new();

	public FileChangeMonitorService(StateStore stateStore, IDbContextFactory<MediaDeckDbContext> dbFactory, ILogger<FileChangeMonitorService> logger, IDispatcherGate dispatcherGate) {
		this._stateStore = stateStore;
		this._dbFactory = dbFactory;
		this._logger = logger;
		this._dispatcherGate = dispatcherGate;

		foreach (var folder in this._stateStore.State.FolderManagerState.Folders) {
			this.AddWatcher(folder.FolderPath);
		}

		this._stateStore.State.FolderManagerState.Folders.ObserveAdd().Subscribe(ev => {
			this.AddWatcher(ev.Value.FolderPath);
		});

		this._stateStore.State.FolderManagerState.Folders.ObserveRemove().Subscribe(ev => {
			this.RemoveWatcher(ev.Value.FolderPath);
		});
	}

	private void AddWatcher(string path) {
		if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path) || this._watchers.ContainsKey(path)) {
			return;
		}

		try {
			var watcher = new FileSystemWatcher(path) {
				IncludeSubdirectories = true,
				NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName
			};
			watcher.Deleted += this.OnFileDeleted;
			watcher.Renamed += this.OnFileRenamed;
			watcher.EnableRaisingEvents = true;

			this._watchers.TryAdd(path, watcher);
		} catch (Exception ex) {
			this._logger.LogError(ex, "Failed to start watching {Path}", path);
		}
	}

	private void RemoveWatcher(string path) {
		if (this._watchers.TryRemove(path, out var watcher)) {
			watcher.EnableRaisingEvents = false;
			watcher.Deleted -= this.OnFileDeleted;
			watcher.Renamed -= this.OnFileRenamed;
			watcher.Dispose();
		}
	}

	private void OnFileDeleted(object sender, FileSystemEventArgs e) {
		_ = this.HandleFileChangeAsync(e.FullPath, null, FileChangeType.Deleted);
	}

	private void OnFileRenamed(object sender, RenamedEventArgs e) {
		_ = this.HandleFileChangeAsync(e.OldFullPath, e.FullPath, FileChangeType.Moved);
	}

	/// <summary>
	/// ファイルの変更イベントを処理します。
	/// </summary>
	/// <param name="oldPath">変更前のパス</param>
	/// <param name="newPath">変更後のパス（移動の場合のみ。削除の場合はnull）</param>
	/// <param name="changeType">変更の種類</param>
	private async Task HandleFileChangeAsync(string oldPath, string? newPath, FileChangeType changeType) {
		try {
			// A -> B, then B -> C という2段階移動（または移動直後の削除）に対応するため、
			// 既存の未処理キューの NewPath が今回の oldPath と一致するものがないか確認する
			var existingByNewPath = this.UnprocessedChanges.FirstOrDefault(c => c.ChangeType == FileChangeType.Moved && c.NewPath == oldPath);
			if (existingByNewPath != null) {
				this.UpdateOrRemoveChangeItem(existingByNewPath, newPath, changeType);
				return;
			}

			await using var db = await this._dbFactory.CreateDbContextAsync();
			// EF Core 経由で DB 内のファイルを検索（oldPath は DB 上のパスと一致するはず）
			var file = await db.MediaFiles.FirstOrDefaultAsync(mf => mf.FilePath == oldPath);
			if (file == null) {
				return;
			}

			// 同一ファイルの変更がすでにキューにあるか確認（IDで紐付け）
			var existingById = this.UnprocessedChanges.FirstOrDefault(c => c.MediaFileId == file.MediaFileId);
			if (existingById != null) {
				this.UpdateOrRemoveChangeItem(existingById, newPath, changeType);
				return;
			}

			var model = new FileChangeItem {
				MediaFileId = file.MediaFileId,
				OldPath = oldPath,
				NewPath = newPath ?? string.Empty,
				ChangeType = changeType
			};

			this._dispatcherGate.BeginInvoke(() => this.UnprocessedChanges.Add(model));
		} catch (Exception ex) {
			this._logger.LogError(ex, "Error handling file change: {Path}", oldPath);
		}
	}

	/// <summary>
	/// 既存の変更アイテムを更新、または不要になった場合は削除します。
	/// </summary>
	private void UpdateOrRemoveChangeItem(FileChangeItem existing, string? nextNewPath, FileChangeType nextChangeType) {
		this._dispatcherGate.BeginInvoke(() => {
			var index = this.UnprocessedChanges.IndexOf(existing);
			if (index < 0) {
				return;
			}

			var combinedNewPath = nextChangeType == FileChangeType.Moved ? (nextNewPath ?? string.Empty) : string.Empty;

			// 移動先が元のパスに戻った場合（A -> B -> A）はキューから削除する
			if (nextChangeType == FileChangeType.Moved && existing.OldPath == combinedNewPath) {
				this.UnprocessedChanges.RemoveAt(index);
				return;
			}

			// アイテムを差し替えてUIに更新を通知する
			var updated = new FileChangeItem {
				MediaFileId = existing.MediaFileId,
				OldPath = existing.OldPath,
				NewPath = combinedNewPath,
				ChangeType = nextChangeType
			};
			this.UnprocessedChanges[index] = updated;
		});
	}

	public void Dispose() {
		foreach (var watcher in this._watchers.Values) {
			watcher.EnableRaisingEvents = false;
			watcher.Deleted -= this.OnFileDeleted;
			watcher.Renamed -= this.OnFileRenamed;
			watcher.Dispose();
		}
		this._watchers.Clear();
	}

	public async Task ApplyChangesAsync(IEnumerable<FileChangeItem> items, bool deleteFromDb) {
		try {
			await using var db = await this._dbFactory.CreateDbContextAsync();
			foreach (var item in items) {
				var file = await db.MediaFiles.FirstOrDefaultAsync(mf => mf.MediaFileId == item.MediaFileId);
				if (file != null) {
					if (deleteFromDb || item.ChangeType == FileChangeType.Deleted) {
						db.MediaFiles.Remove(file);
					} else if (item.ChangeType == FileChangeType.Moved) {
						file.FilePath = item.NewPath;
					}
				}
			}
			await db.SaveChangesAsync();

			this.RemoveItemsFromList(items);
		} catch (Exception ex) {
			this._logger.LogError(ex, "Error applying file changes to DB");
		}
	}

	public void DiscardChanges(IEnumerable<FileChangeItem> items) {
		this.RemoveItemsFromList(items);
	}

	/// <summary>
	/// リストから指定されたアイテムを削除します。
	/// </summary>
	private void RemoveItemsFromList(IEnumerable<FileChangeItem> items) {
		this._dispatcherGate.BeginInvoke(() => {
			var targetElement = this.UnprocessedChanges.Where(x => items.Any(i => i.MediaFileId == x.MediaFileId));
			this.UnprocessedChanges.RemoveRange(targetElement);
		});
	}
}
