using MediaDeck.Composition.Database;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;
using MediaDeck.Composition.Objects;
using MediaDeck.Composition.Tables;
using MediaDeck.Core.Models.NotificationDispatcher;

namespace MediaDeck.Core.Models.Files;

/// <summary>
/// ファイルの管理を行うクラス
/// </summary>
[Inject(InjectServiceLifetime.Transient)]
public class FilesManager {
	private readonly IDbContextFactory<MediaDeckDbContext> _dbFactory;
	private readonly AppNotificationDispatcher _notificationDispatcher;

	/// <summary>
	/// FilesManagerクラスの新しいインスタンスを初期化
	/// </summary>
	/// <param name="dbFactory">データベースコンテキストファクトリー</param>
	/// <param name="notificationDispatcher">通知ディスパッチャー</param>
	public FilesManager(IDbContextFactory<MediaDeckDbContext> dbFactory, AppNotificationDispatcher notificationDispatcher) {
		this._dbFactory = dbFactory;
		this._notificationDispatcher = notificationDispatcher;
	}

	/// <summary>
	/// 指定されたファイルをデータベースから削除し、完了通知を発行
	/// </summary>
	/// <param name="fileModels">削除するファイルモデルのコレクション</param>
	public async Task RemoveFilesAsync(IEnumerable<IMediaItemModel> fileModels) {
		await using var db = await this._dbFactory.CreateDbContextAsync();
		using var transaction = await db.Database.BeginTransactionAsync();
		var ids = fileModels.Select(x => x.Id).ToArray();
		var targetFiles =
			await db
				.MediaItems
				.Where(x => ids.Contains(x.MediaItemId))
				.ToListAsync();

		if (targetFiles.Count == 0) {
			return;
		}

		db.MediaItems.RemoveRange(targetFiles);

		var folderGroups = targetFiles.Where(x => x.ItemType == ItemType.FolderGroup).Select(fg => fg.FilePath).ToArray();
		if (folderGroups.Any()) {
			var targets = db.MediaItems.Where(x => folderGroups.Any(fg => fg == x.DirectoryPath || x.DirectoryPath.StartsWith(fg + Path.DirectorySeparatorChar))).ToArray();
			foreach (var target in targets) {
				target.IsUnderFolderGroup = db.MediaItems.Any(fg => target.DirectoryPath == fg.FilePath && target.DirectoryPath.StartsWith(fg.FilePath + Path.DirectorySeparatorChar));
			}
			db.UpdateRange(targets);
		}

		await db.SaveChangesAsync();
		await transaction.CommitAsync();

		var message = targetFiles.Count == 1 ? "File removed from MediaDeck database" : $"{targetFiles.Count} files removed from MediaDeck database";
		this._notificationDispatcher.Notify.OnNext(AppNotification.Success(message));
	}
}