using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.DependencyInjection;

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
	public long MediaFileId { get; set; }
	public string OldPath { get; set; } = string.Empty;
	public string NewPath { get; set; } = string.Empty;
	public FileChangeType ChangeType { get; set; }

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
	private readonly ConcurrentDictionary<string, FileSystemWatcher> _watchers = new();

	public ObservableList<FileChangeItem> UnprocessedChanges { get; } = new();

	public FileChangeMonitorService(StateStore stateStore, IDbContextFactory<MediaDeckDbContext> dbFactory, ILogger<FileChangeMonitorService> logger) {
		this._stateStore = stateStore;
		this._dbFactory = dbFactory;
		this._logger = logger;

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

	private async Task HandleFileChangeAsync(string oldPath, string? newPath, FileChangeType changeType) {
		try {
			await using var db = await this._dbFactory.CreateDbContextAsync();
			var file = await db.MediaFiles.FirstOrDefaultAsync(mf => mf.FilePath == oldPath);
			if (file == null) {
				return;
			}

			if (this.UnprocessedChanges.Any(c => c.MediaFileId == file.MediaFileId)) {
				return;
			}

			var model = new FileChangeItem {
				MediaFileId = file.MediaFileId,
				OldPath = oldPath,
				NewPath = newPath ?? string.Empty,
				ChangeType = changeType
			};

			var mainWindow = Ioc.Default.GetService<MainWindow>();
			if (mainWindow?.DispatcherQueue != null) {
				mainWindow.DispatcherQueue.TryEnqueue(() => {
					this.UnprocessedChanges.Add(model);
				});
			} else {
				this.UnprocessedChanges.Add(model);
			}
		} catch (Exception ex) {
			this._logger.LogError(ex, "Error handling file change: {Path}", oldPath);
		}
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

	private void RemoveItemsFromList(IEnumerable<FileChangeItem> items) {
		var mainWindow = Ioc.Default.GetService<MainWindow>();
		Action action = () => {
			foreach (var item in items) {
				var targetElement = this.UnprocessedChanges.FirstOrDefault(x => x.MediaFileId == item.MediaFileId);
				if (targetElement != null) {
					this.UnprocessedChanges.Remove(targetElement);
				}
			}
		};

		if (mainWindow?.DispatcherQueue != null) {
			mainWindow.DispatcherQueue.TryEnqueue(() => action());
		} else {
			action();
		}
	}
}
