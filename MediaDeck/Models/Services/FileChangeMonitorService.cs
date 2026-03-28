#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using R3;

using MediaDeck.Composition.Interfaces;
using MediaDeck.Composition.Objects;
using MediaDeck.Database;
using MediaDeck.Stores.State;
using MediaDeck.Utils.Notifications;
using MediaDeck.Utils.Tools;
using MediaDeck.Models.Files;

using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using MediaDeck.Common.Base;

namespace MediaDeck.Models.Services;

/// <summary>
/// ファイルシステムの変更を監視し、DBとの同期を管理するサービスです。
/// FileSystemWatcherManager / FileChangeTracker に責務を委譲するオーケストレーターとして機能します。
/// </summary>
[Inject(InjectServiceLifetime.Singleton)]
public class FileChangeMonitorService : ModelBase {
	private readonly IDbContextFactory<MediaDeckDbContext> _dbFactory;
	private readonly ILogger<FileChangeMonitorService> _logger;
	private readonly AppNotificationDispatcher _appNotificationDispatcher;
	private readonly FileRegistrar _fileRegistrar;
	private readonly FileSystemWatcherManager _watcherManager;

	/// <summary>
	/// 未処理の変更リストを管理するトラッカー
	/// </summary>
	public FileChangeTracker Tracker {
		get;
	}

	/// <summary>
	/// FileChangeMonitorServiceクラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="stateStore">状態管理ストア</param>
	/// <param name="dbFactory">DBコンテキストファクトリー</param>
	/// <param name="tracker">ファイル変更トラッカー</param>
	/// <param name="logger">ロガー</param>
	/// <param name="dispatcherGate">UIスレッドディスパッチャー</param>
	/// <param name="appNotificationDispatcher">通知送信クラス</param>
	/// <param name="fileRegistrar">ファイル登録クラス</param>
	public FileChangeMonitorService(StateStore stateStore, IDbContextFactory<MediaDeckDbContext> dbFactory, FileChangeTracker tracker, ILogger<FileChangeMonitorService> logger, AppNotificationDispatcher appNotificationDispatcher, FileRegistrar fileRegistrar) {
		this._dbFactory = dbFactory;
		this._logger = logger;
		this._appNotificationDispatcher = appNotificationDispatcher;
		this._fileRegistrar = fileRegistrar;

		this.Tracker = tracker;
		this.Tracker.AddTo(this.CompositeDisposable);

		this._watcherManager = new FileSystemWatcherManager(
			logger,
			onFileDeleted: path => this.Tracker.OnDeleted(path),
			onFileRenamed: (oldPath, newPath) => this.Tracker.OnRenamed(oldPath, newPath),
			onFileCreated: path => this.Tracker.OnCreated(path),
			onFileChanged: path => this.Tracker.OnChanged(path),
			onError: this.HandleWatcherError
		);

		// 初期フォルダーの監視を開始
		foreach (var folder in stateStore.State.FolderManagerState.Folders) {
			this._watcherManager.AddWatcher(folder.FolderPath);
		}

		// フォルダー追加の購読
		stateStore.State.FolderManagerState.Folders.ObserveAdd().Subscribe(ev => {
			this._watcherManager.AddWatcher(ev.Value.FolderPath);
		}).AddTo(this.CompositeDisposable);

		// フォルダー削除の購読
		stateStore.State.FolderManagerState.Folders.ObserveRemove().Subscribe(ev => {
			this._watcherManager.RemoveWatcher(ev.Value.FolderPath);
		}).AddTo(this.CompositeDisposable);
	}

	/// <summary>
	/// Watcherエラーを処理します。バッファオーバーフロー時にユーザーへ通知します。
	/// </summary>
	/// <param name="ex">発生した例外</param>
	private void HandleWatcherError(Exception ex) {
		if (ex is InternalBufferOverflowException) {
			var notif = AppNotification.Warning("ファイルの変更監視が追いつきませんでした。最新状態を反映するには、手動でフォルダの再読み込みを実行してください。", "監視エラー", 10000);
			this._appNotificationDispatcher.Notify.OnNext(notif);
		}
	}


	/// <summary>
	/// 変更内容をデータベースに反映します。
	/// </summary>
	/// <param name="items">反映対象のアイテム一覧</param>
	/// <param name="deleteFromDb">強制的に削除するかどうか</param>
	public async Task ApplyChangesAsync(System.Collections.Generic.IEnumerable<FileChangeItem> items, bool deleteFromDb) {
		try {
			await using var db = await this._dbFactory.CreateDbContextAsync();
			foreach (var item in items) {
				if (item.ChangeType == FileChangeType.Added) {
					// 新規追加アイテムが承認された場合はFileRegistrarへ回す
					this._fileRegistrar.RegistrationQueue.Enqueue(item.NewPath);
					continue;
				}

				if (item.MediaFileId.HasValue) {
					var file = await db.MediaFiles.FirstOrDefaultAsync(mf => mf.MediaFileId == item.MediaFileId.Value);
					if (file != null) {
						if (deleteFromDb || item.ChangeType == FileChangeType.Deleted) {
							db.MediaFiles.Remove(file);
						} else if (item.ChangeType == FileChangeType.Moved || item.ChangeType == FileChangeType.Renamed) {
							file.FilePath = item.NewPath;
						}
					}
				}
			}
			await db.SaveChangesAsync();

			this.Tracker.RemoveItems(items);
		} catch (Exception ex) {
			this._logger.LogError(ex, "Error applying file changes to DB");
		}
	}

	/// <summary>
	/// 未処理変更として提示された内容を破棄（無視）します。
	/// </summary>
	/// <param name="items">破棄対象のアイテム一覧</param>
	public void DiscardChanges(System.Collections.Generic.IEnumerable<FileChangeItem> items) {
		this.Tracker.RemoveItems(items);
	}

	/// <summary>
	/// リソースを破棄します。
	/// </summary>
	/// <param name="disposing">マネージドリソースを破棄するかどうか</param>
	protected override void Dispose(bool disposing) {
		if (disposing) {
			this._watcherManager.Dispose();
		}
		base.Dispose(disposing);
	}
}
