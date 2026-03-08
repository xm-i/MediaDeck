using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using MediaDeck.Composition.Interfaces.FileTypes.Models;
using MediaDeck.Composition.Objects;
using MediaDeck.Database;
using MediaDeck.Utils.Constants;
using MediaDeck.Utils.Notifications;

namespace MediaDeck.Models.FileDetailManagers;

/// <summary>
/// ファイルの管理を行うクラス
/// </summary>
[Inject(InjectServiceLifetime.Transient)]
public class FilesManager {
	private readonly MediaDeckDbContext _db;
	private readonly AppNotificationDispatcher _notificationDispatcher;

	/// <summary>
	/// FilesManagerクラスの新しいインスタンスを初期化
	/// </summary>
	/// <param name="db">データベースコンテキスト</param>
	/// <param name="notificationDispatcher">通知ディスパッチャー</param>
	public FilesManager(MediaDeckDbContext db, AppNotificationDispatcher notificationDispatcher) {
		this._db = db;
		this._notificationDispatcher = notificationDispatcher;
	}

	/// <summary>
	/// 指定されたファイルをデータベースから削除し、完了通知を発行
	/// </summary>
	/// <param name="fileModels">削除するファイルモデルのコレクション</param>
	public async Task RemoveFilesAsync(IEnumerable<IFileModel> fileModels) {
		using var lockObject = await LockObjectConstants.DbLock.LockAsync();
		using var transaction = await this._db.Database.BeginTransactionAsync();
		var ids = fileModels.Select(x => x.Id).ToArray();
		var targetFiles =
			await this._db
				.MediaFiles
				.Where(x => ids.Contains(x.MediaFileId))
				.ToListAsync();

		if (targetFiles.Count == 0) {
			return;
		}
		this._db.MediaFiles.RemoveRange(targetFiles);
		await this._db.SaveChangesAsync();
		await transaction.CommitAsync();

		var message = targetFiles.Count == 1
			? "File removed from MediaDeck database"
			: $"{targetFiles.Count} files removed from MediaDeck database";
		this._notificationDispatcher.Notify.OnNext(AppNotification.Success(message));
	}
}
