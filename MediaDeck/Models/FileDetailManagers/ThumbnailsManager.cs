using System.IO;
using System.Threading.Tasks;

using MediaDeck.Composition.Interfaces.FileTypes.Models;
using MediaDeck.Database;
using MediaDeck.Utils.Constants;

namespace MediaDeck.Models.FileDetailManagers;

[Inject(InjectServiceLifetime.Singleton)]
public class ThumbnailsManager(MediaDeckDbContext dbContext) {
	private readonly MediaDeckDbContext _db = dbContext;

	public async Task UpdateThumbnailAsync(IFileModel fileModel, byte[] thumbnail) {
		var thumbRelativePath = FilePathUtility.GetThumbnailRelativeFilePath(fileModel.FilePath);
		var thumbPath = FilePathUtility.GetThumbnailAbsoluteFilePath(thumbRelativePath);
		await File.WriteAllBytesAsync(thumbPath, thumbnail);

		if (fileModel.ThumbnailFilePath == thumbPath) {
			return;
		}
		using var lockObject = await LockObjectConstants.DbLock.LockAsync();
		using var transaction = await this._db.Database.BeginTransactionAsync();
		var mf = await this._db.MediaFiles.FirstAsync(x => x.MediaFileId== fileModel.Id);
		mf.ThumbnailFileName = thumbPath;
		this._db.MediaFiles.Update(mf);

		await this._db.SaveChangesAsync();
		await transaction.CommitAsync();
	}

	public async Task<byte[]?> LoadThumbnailAsync(IFileModel fileModel) {
		if (fileModel.ThumbnailFilePath is not { } path) {
			return null;
		}
		return await File.ReadAllBytesAsync(path);
	}
}
