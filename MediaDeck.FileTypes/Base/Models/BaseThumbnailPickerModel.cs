using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using MediaDeck.Composition.Interfaces.FileTypes.Models;
using MediaDeck.Database;

using Microsoft.Extensions.Logging;

namespace MediaDeck.FileTypes.Base.Models;

public class BaseThumbnailPickerModel(IDbContextFactory<MediaDeckDbContext> dbFactory, ILogger<BaseThumbnailPickerModel> logger, IFilePathService filePathService) {
	private readonly IDbContextFactory<MediaDeckDbContext> _dbFactory = dbFactory;
	private readonly IFilePathService _filePathService = filePathService;

	public async Task UpdateThumbnailAsync(IFileModel fileModel, byte[] thumbnail) {
		var thumbRelativePath = this._filePathService.GetThumbnailRelativeFilePath(fileModel.FilePath);
		var thumbPath = this._filePathService.GetThumbnailAbsoluteFilePath(thumbRelativePath);
		await File.WriteAllBytesAsync(thumbPath, thumbnail);

		if (fileModel.ThumbnailFilePath == thumbPath) {
			return;
		}

		await using var db = await this._dbFactory.CreateDbContextAsync();
		using var transaction = await db.Database.BeginTransactionAsync();
		var mf = await db.MediaFiles.FirstAsync(x => x.MediaFileId == fileModel.Id);
		mf.ThumbnailFileName = thumbPath;
		db.MediaFiles.Update(mf);

		await db.SaveChangesAsync();
		await transaction.CommitAsync();
	}

	public async Task<byte[]?> LoadThumbnailAsync(IFileModel fileModel) {
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