using System.Collections.Concurrent;
using System.IO;

using Microsoft.Extensions.Logging;

namespace MediaDeck.Core.Models.Services;

/// <summary>
/// FileSystemWatcherの生成・破棄・イベント登録を一元管理するクラスです。
/// </summary>
public class FileSystemWatcherManager : IDisposable {
	private readonly ConcurrentDictionary<string, FileSystemWatcher> _watchers = new();
	private readonly ILogger _logger;
	private readonly Action<string> _onFileDeleted;
	private readonly Action<string, string> _onFileRenamed;
	private readonly Action<string> _onFileCreated;
	private readonly Action<string> _onFileChanged;
	private readonly Action<Exception> _onError;

	/// <summary>
	/// FileSystemWatcherManagerクラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="logger">ロガー</param>
	/// <param name="onFileDeleted">ファイル削除時のコールバック</param>
	/// <param name="onFileRenamed">ファイルリネーム時のコールバック</param>
	/// <param name="onFileCreated">ファイル作成時のコールバック</param>
	/// <param name="onFileChanged">ファイル変更時のコールバック</param>
	/// <param name="onError">エラー発生時のコールバック</param>
	public FileSystemWatcherManager(
		ILogger logger,
		Action<string> onFileDeleted,
		Action<string, string> onFileRenamed,
		Action<string> onFileCreated,
		Action<string> onFileChanged,
		Action<Exception> onError) {
		this._logger = logger;
		this._onFileDeleted = onFileDeleted;
		this._onFileRenamed = onFileRenamed;
		this._onFileCreated = onFileCreated;
		this._onFileChanged = onFileChanged;
		this._onError = onError;
	}

	/// <summary>
	/// 指定されたパスのディレクトリ監視を開始します。
	/// </summary>
	/// <param name="path">監視対象パス</param>
	public void AddWatcher(string path) {
		if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path) || this._watchers.ContainsKey(path)) {
			return;
		}

		try {
			var watcher = new FileSystemWatcher(path) {
				IncludeSubdirectories = true, NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite | NotifyFilters.Size, InternalBufferSize = 65536 // OS最大値 (64KB) を設定してイベント溢れを防ぐ
			};
			watcher.Deleted += this.OnFileDeleted;
			watcher.Renamed += this.OnFileRenamed;
			watcher.Created += this.OnFileCreated;
			watcher.Changed += this.OnFileChanged;
			watcher.Error += this.OnWatcherError;
			watcher.EnableRaisingEvents = true;

			this._watchers.TryAdd(path, watcher);
		} catch (Exception ex) {
			this._logger.LogError(ex, "Failed to start watching {Path}", path);
		}
	}

	/// <summary>
	/// 指定されたパスのディレクトリ監視を停止・削除します。
	/// </summary>
	/// <param name="path">監視停止パス</param>
	public void RemoveWatcher(string path) {
		if (this._watchers.TryRemove(path, out var watcher)) {
			this.DisposeWatcher(watcher);
		}
	}

	/// <summary>
	/// 個々のWatcherのリソースを解放します。
	/// </summary>
	/// <param name="watcher">解放対象のFileSystemWatcher</param>
	private void DisposeWatcher(FileSystemWatcher watcher) {
		watcher.EnableRaisingEvents = false;
		watcher.Deleted -= this.OnFileDeleted;
		watcher.Renamed -= this.OnFileRenamed;
		watcher.Created -= this.OnFileCreated;
		watcher.Changed -= this.OnFileChanged;
		watcher.Error -= this.OnWatcherError;
		watcher.Dispose();
	}

	private void OnFileDeleted(object sender, FileSystemEventArgs e) {
		this._onFileDeleted(e.FullPath);
	}

	private void OnFileRenamed(object sender, RenamedEventArgs e) {
		this._onFileRenamed(e.OldFullPath, e.FullPath);
	}

	private void OnFileCreated(object sender, FileSystemEventArgs e) {
		this._onFileCreated(e.FullPath);
	}

	private void OnFileChanged(object sender, FileSystemEventArgs e) {
		this._onFileChanged(e.FullPath);
	}

	private void OnWatcherError(object sender, ErrorEventArgs e) {
		this._onError(e.GetException());
	}

	/// <summary>
	/// すべてのWatcherを停止してリソースを解放します。
	/// </summary>
	public void Dispose() {
		foreach (var watcher in this._watchers.Values) {
			this.DisposeWatcher(watcher);
		}
		this._watchers.Clear();
	}
}