using System.Threading.Tasks;

using MediaDeck.Composition.Interfaces.FileTypes.Models;
using MediaDeck.Database;
using MediaDeck.Utils.Constants;

namespace MediaDeck.Models.FileDetailManagers;

[Inject(InjectServiceLifetime.Transient)]
public class FilesManager {
	public FilesManager(MediaDeckDbContext db) {
		this._db = db;
	}
	private readonly MediaDeckDbContext _db;

	public async Task RemoveFileAsync(IFileModel fileModel) {
		using var lockObject = await LockObjectConstants.DbLock.LockAsync();
		using var transaction = await this._db.Database.BeginTransactionAsync();
		var targetFile =
			await this._db
				.MediaFiles
				.FirstOrDefaultAsync(x => x.MediaFileId == fileModel.Id);

		if(targetFile == null) {
			return;
		}
		this._db.MediaFiles.Remove(targetFile);
		await this._db.SaveChangesAsync();
		await transaction.CommitAsync();
	}
}
