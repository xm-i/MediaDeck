using MediaDeck.Composition.Database;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.Services;
using MediaDeck.Composition.Tables;
using MediaDeck.Core.Services.FileStatusUpdator;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Shouldly;

namespace MediaDeck.Core.Tests.Services.FileStatusUpdator;

/// <summary>
/// <see cref="FileStatusUpdatorService"/> のユニットテスト。
/// 責務: データベース上のメディアアイテム情報を物理ファイルの状態（存在有無、ファイルサイズ、更新日時など）と同期させ、
/// 変更が検知された場合はハッシュ値（PreHash/FullHash）の再計算をキューに登録すること。
/// </summary>
public sealed class FileStatusUpdatorServiceTests : IDisposable {
	private SqliteConnection? _connection;

	private IDbContextFactory<MediaDeckDbContext> CreateInMemoryDbFactory() {
		this._connection = new SqliteConnection("DataSource=:memory:");
		this._connection.Open();

		var options = new DbContextOptionsBuilder<MediaDeckDbContext>()
			.UseSqlite(this._connection)
			.Options;

		using (var context = new MediaDeckDbContext(options)) {
			context.Database.EnsureCreated();
		}

		var factoryMock = new Mock<IDbContextFactory<MediaDeckDbContext>>();
		factoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(() => new MediaDeckDbContext(options));

		return factoryMock.Object;
	}

	public void Dispose() {
		this._connection?.Dispose();
	}

	/// <summary>
	/// テスト用のデータベース書き込み直列化サービスを生成します。
	/// </summary>
	/// <returns>即時実行するモックサービス</returns>
	private static IDatabaseWriteCoordinator CreateDatabaseWriteCoordinator() {
		var databaseWriteCoordinatorMock = new Mock<IDatabaseWriteCoordinator>();
		databaseWriteCoordinatorMock
			.Setup(x => x.ExecuteAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
			.Returns<Func<CancellationToken, Task>, CancellationToken>((operation, ct) => operation(ct));

		return databaseWriteCoordinatorMock.Object;
	}

	[Fact]
	public async Task UpdateFileInfo_FileNotModified_ShouldNotQueueHashUpdate() {
		// Arrange
		var dbFactory = this.CreateInMemoryDbFactory();
		var hashUpdatorMock = new Mock<IFileHashUpdatorService>();
		var mediaTypeServiceMock = new Mock<IMediaItemTypeService>();
		var providerMock = new Mock<IMediaItemTypeProvider>();

		var now = DateTime.Now;
		var filePath = "C:\\test\\test.jpg";

		await using (var db = await dbFactory.CreateDbContextAsync()) {
			db.MediaItems.Add(new MediaItem {
				MediaItemId = 1,
				MediaType = MediaType.Image,
				FilePath = filePath,
				IsExists = true,
				FileSize = 1000,
				CreationTime = now,
				ModifiedTime = now,
				LastAccessTime = now,
				PreHashUpdatedTime = now,
				DirectoryPath = "C:\\test",
				Description = "",
				IsUnderFolderGroup = false
			});
			await db.SaveChangesAsync();
		}

		providerMock.Setup(x => x.GetPathStatus(filePath)).Returns(new MediaItemPathStatus(
			exists: true,
			fileSize: 1000,
			creationTime: now,
			modifiedTime: now,
			lastAccessTime: now
		));
		mediaTypeServiceMock.Setup(x => x.GetMediaItemTypeProvider(MediaType.Image)).Returns(providerMock.Object);

		var service = new FileStatusUpdatorService(dbFactory, hashUpdatorMock.Object, mediaTypeServiceMock.Object, CreateDatabaseWriteCoordinator());

		// Act
		await service.UpdateFileInfo();

		// Assert
		hashUpdatorMock.Verify(x => x.EnqueueHashUpdate(It.IsAny<long>()), Times.Never);
		hashUpdatorMock.Verify(x => x.CheckAndEnqueueFullHashUpdatesAsync(It.IsAny<CancellationToken>()), Times.Once);

		await using (var db = await dbFactory.CreateDbContextAsync()) {
			var item = await db.MediaItems.FindAsync(1L);
			item!.FileSize.ShouldBe(1000);
			item.IsExists.ShouldBeTrue();
		}
	}

	[Fact]
	public async Task UpdateFileInfo_FileModified_ShouldQueueHashUpdateAndUpdateDb() {
		// Arrange
		var dbFactory = this.CreateInMemoryDbFactory();
		var hashUpdatorMock = new Mock<IFileHashUpdatorService>();
		var mediaTypeServiceMock = new Mock<IMediaItemTypeService>();
		var providerMock = new Mock<IMediaItemTypeProvider>();

		var oldTime = DateTime.Now.AddDays(-1);
		var newTime = DateTime.Now;
		var filePath = "C:\\test\\modified.jpg";

		await using (var db = await dbFactory.CreateDbContextAsync()) {
			db.MediaItems.Add(new MediaItem {
				MediaItemId = 2,
				MediaType = MediaType.Image,
				FilePath = filePath,
				IsExists = true,
				FileSize = 1000,
				CreationTime = oldTime,
				ModifiedTime = oldTime,
				LastAccessTime = oldTime,
				PreHashUpdatedTime = oldTime,
				DirectoryPath = "C:\\test",
				Description = "",
				IsUnderFolderGroup = false
			});
			await db.SaveChangesAsync();
		}

		providerMock.Setup(x => x.GetPathStatus(filePath)).Returns(new MediaItemPathStatus(
			exists: true,
			fileSize: 2000,
			creationTime: oldTime,
			modifiedTime: newTime,
			lastAccessTime: newTime
		));
		mediaTypeServiceMock.Setup(x => x.GetMediaItemTypeProvider(MediaType.Image)).Returns(providerMock.Object);

		var service = new FileStatusUpdatorService(dbFactory, hashUpdatorMock.Object, mediaTypeServiceMock.Object, CreateDatabaseWriteCoordinator());

		// Act
		await service.UpdateFileInfo();

		// Assert
		hashUpdatorMock.Verify(x => x.EnqueueHashUpdate(2L), Times.Once);
		hashUpdatorMock.Verify(x => x.CheckAndEnqueueFullHashUpdatesAsync(It.IsAny<CancellationToken>()), Times.Once);

		await using (var db = await dbFactory.CreateDbContextAsync()) {
			var item = await db.MediaItems.FindAsync(2L);
			item!.FileSize.ShouldBe(2000);
			item.ModifiedTime.ShouldBe(newTime);
			item.IsExists.ShouldBeTrue();
		}
	}

	[Fact]
	public async Task UpdateFileInfo_FileDeleted_ShouldUpdateIsExistsToFalse() {
		// Arrange
		var dbFactory = this.CreateInMemoryDbFactory();
		var hashUpdatorMock = new Mock<IFileHashUpdatorService>();
		var mediaTypeServiceMock = new Mock<IMediaItemTypeService>();
		var providerMock = new Mock<IMediaItemTypeProvider>();

		var now = DateTime.Now;
		var filePath = "C:\\test\\deleted.jpg";

		await using (var db = await dbFactory.CreateDbContextAsync()) {
			db.MediaItems.Add(new MediaItem {
				MediaItemId = 3,
				MediaType = MediaType.Image,
				FilePath = filePath,
				IsExists = true,
				FileSize = 1000,
				CreationTime = now,
				ModifiedTime = now,
				LastAccessTime = now,
				PreHashUpdatedTime = now,
				DirectoryPath = "C:\\test",
				Description = "",
				IsUnderFolderGroup = false
			});
			await db.SaveChangesAsync();
		}

		providerMock.Setup(x => x.GetPathStatus(filePath)).Returns(new MediaItemPathStatus(
			exists: false,
			fileSize: 0,
			creationTime: default,
			modifiedTime: default,
			lastAccessTime: default
		));
		mediaTypeServiceMock.Setup(x => x.GetMediaItemTypeProvider(MediaType.Image)).Returns(providerMock.Object);

		var service = new FileStatusUpdatorService(dbFactory, hashUpdatorMock.Object, mediaTypeServiceMock.Object, CreateDatabaseWriteCoordinator());

		// Act
		await service.UpdateFileInfo();

		// Assert
		hashUpdatorMock.Verify(x => x.EnqueueHashUpdate(It.IsAny<long>()), Times.Never);
		hashUpdatorMock.Verify(x => x.CheckAndEnqueueFullHashUpdatesAsync(It.IsAny<CancellationToken>()), Times.Once);

		await using (var db = await dbFactory.CreateDbContextAsync()) {
			var item = await db.MediaItems.FindAsync(3L);
			item!.IsExists.ShouldBeFalse();
		}
	}
}