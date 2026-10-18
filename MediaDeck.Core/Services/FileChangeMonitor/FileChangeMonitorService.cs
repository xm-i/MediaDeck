using MediaDeck.Common.Base;
using MediaDeck.Composition.Database;
using MediaDeck.Composition.Objects;
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
	public FileChangeMonitorService(IDbContextFactory<MediaDeckDbContext> dbFactory, FileChangeTracker tracker, ILogger<FileChangeMonitorService> logger, AppNotificationDispatcher appNotificationDispatcher, FileRegistrar fileRegistrar, IDatabaseWriteCoordinator databaseWriteCoordinator) {
		this._dbFactory = dbFactory;
		this._databaseWriteCoordinator = databaseWriteCoordinator;
		this._logger = logger;
		this._appNotificationDispatcher = appNotificationDispatcher;
		this._fileRegistrar = fileRegistrar;

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
			var notif = AppNotification.Warning("ファイルの変更監視が追いつきませんでした。最新状態を反映するには、手動でフォルダの再読み込みを実行してください。", "監視エラー", 10000);
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
		try {
			foreach (var item in targetItems.Where(static x => x.ChangeType == FileChangeType.Added)) {
				// 新規追加アイテムが承認された場合はFileRegistrarへ回す
				this._fileRegistrar.Enqueue(item.NewPath);
			}

			var dbUpdateItems = targetItems
				.Where(static x => x.ChangeType != FileChangeType.Added)
				.ToArray();

			if (dbUpdateItems.Length > 0) {
				await this._databaseWriteCoordinator.ExecuteAsync(async ct => {
					await using var db = await this._dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
					foreach (var item in dbUpdateItems) {
						if (item.MediaItemId.HasValue) {
							var file = await db.MediaItems.FirstOrDefaultAsync(mf => mf.MediaItemId == item.MediaItemId.Value, ct).ConfigureAwait(false);
							if (file != null) {
								if (deleteFromDb || item.ChangeType == FileChangeType.Deleted) {
									db.MediaItems.Remove(file);
								} else if (item.ChangeType == FileChangeType.Moved || item.ChangeType == FileChangeType.Renamed) {
									file.FilePath = item.NewPath;
									file.DirectoryPath = Path.GetDirectoryName(item.NewPath) ?? string.Empty;
								}
							}
						}
					}
					await db.SaveChangesAsync(ct).ConfigureAwait(false);
				}).ConfigureAwait(false);
			}

			this.Tracker.RemoveItems(targetItems);
		} catch (Exception ex) {
			this._logger.LogError(ex, "Error applying file changes to DB");
		}
	}

	/// <summary>
	/// 未処理変更として提示された内容を破棄（無視）します。
	/// </summary>
	/// <param name="items">破棄対象のアイテム一覧</param>
	public void DiscardChanges(IEnumerable<FileChangeItem> items) {
		this.Tracker.RemoveItems(items);
	}
}