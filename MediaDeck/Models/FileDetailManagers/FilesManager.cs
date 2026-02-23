using System.Collections.Generic;
using System.Linq;
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
	}
}
