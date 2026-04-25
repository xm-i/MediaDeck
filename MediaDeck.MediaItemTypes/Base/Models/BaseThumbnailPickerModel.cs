using System.IO;
using System.Threading.Tasks;

using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;
using MediaDeck.Database;

using Microsoft.Extensions.Logging;

namespace MediaDeck.MediaItemTypes.Base.Models;

[Inject(InjectServiceLifetime.Transient)]
internal class BaseThumbnailPickerModel(IDbContextFactory<MediaDeckDbContext> dbFactory, ILogger<BaseThumbnailPickerModel> logger, IFilePathService filePathService) {
	private readonly IDbContextFactory<MediaDeckDbContext> _dbFactory = dbFactory;
	private readonly IFilePathService _filePathService = filePathService;

	internal async Task UpdateThumbnailAsync(IMediaItemModel fileModel, byte[] thumbnail) {
		var thumbRelativePath = this._filePathService.GetThumbnailRelativeFilePath();
		var thumbPath = this._filePathService.GetThumbnailAbsoluteFilePath(thumbRelativePath);
		await File.WriteAllBytesAsync(thumbPath, thumbnail);

		if (fileModel.ThumbnailFilePath == thumbPath) {
			return;
		}

		await using var db = await this._dbFactory.CreateDbContextAsync();
		using var transaction = await db.Database.BeginTransactionAsync();
		var mf = await db.MediaItems.FirstAsync(x => x.MediaItemId == fileModel.Id);
		mf.ThumbnailFileName = thumbPath;
		db.MediaItems.Update(mf);

		await db.SaveChangesAsync();
		await transaction.CommitAsync();
	}

	internal async Task<byte[]?> LoadThumbnailAsync(IMediaItemModel fileModel) {
		if (fileModel.ThumbnailFilePath is not { } path) {
			return null;
		}
		try {
			return await File.ReadAllBytesAsync(path);
		} catch (Exception ex) {
			logger.LogError(ex, "Failed to load thumbnail for file {FileId} at path {ThumbnailPath}", fileModel.Id, path);
			return null;
		}
	}
}