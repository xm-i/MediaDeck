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
	/// テスト用のデータベース書き込み直列化サービスのモックを生成します。
	/// </summary>
	/// <returns>即時実行するモックオブジェクト</returns>
	private static Mock<IDatabaseWriteCoordinator> CreateDatabaseWriteCoordinatorMock() {
		var databaseWriteCoordinatorMock = new Mock<IDatabaseWriteCoordinator>();
		databaseWriteCoordinatorMock
			.Setup(x => x.ExecuteAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
			.Returns<Func<CancellationToken, Task>, CancellationToken>((operation, ct) => operation(ct));

		return databaseWriteCoordinatorMock;
	}

	/// <summary>
	/// テスト用のデータベース書き込み直列化サービスを生成します。
	/// </summary>
	/// <returns>即時実行するモックサービス</returns>
	private static IDatabaseWriteCoordinator CreateDatabaseWriteCoordinator() {
		return CreateDatabaseWriteCoordinatorMock().Object;
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

	/// <summary>
	/// 属性未変更かつPreHashUpdatedTimeがnullのFolderGroupに対してDB更新もハッシュ更新も行われないことを検証します。
	/// </summary>
	[Fact]
	public async Task UpdateFileInfo_FolderGroupUnchangedWithNullPreHash_ShouldNotUpdateDbOrQueueHash() {
		// Arrange
		var dbFactory = this.CreateInMemoryDbFactory();
		var hashUpdatorMock = new Mock<IFileHashUpdatorService>();
		var mediaTypeServiceMock = new Mock<IMediaItemTypeService>();
		var providerMock = new Mock<IMediaItemTypeProvider>();
		var databaseWriteCoordinatorMock = CreateDatabaseWriteCoordinatorMock();

		var now = new DateTime(2026, 11, 21, 10, 0, 0, DateTimeKind.Local);
		var folderPath = "C:\\test\\group";

		await using (var db = await dbFactory.CreateDbContextAsync()) {
			db.MediaItems.Add(new MediaItem {
				MediaItemId = 4,
				MediaType = MediaType.FolderGroup,
				FilePath = folderPath,
				IsExists = true,
				FileSize = 0,
				CreationTime = now,
				ModifiedTime = now,
				LastAccessTime = now,
				PreHashUpdatedTime = null,
				DirectoryPath = "C:\\test",
				Description = "",
				IsUnderFolderGroup = false
			});
			await db.SaveChangesAsync();
		}

		providerMock.Setup(x => x.GetPathStatus(folderPath)).Returns(new MediaItemPathStatus(
			exists: true,
			fileSize: 0,
			creationTime: now,
			modifiedTime: now,
			lastAccessTime: now
		));
		mediaTypeServiceMock.Setup(x => x.GetMediaItemTypeProvider(MediaType.FolderGroup)).Returns(providerMock.Object);

		var service = new FileStatusUpdatorService(dbFactory, hashUpdatorMock.Object, mediaTypeServiceMock.Object, databaseWriteCoordinatorMock.Object);

		// Act
		await service.UpdateFileInfo();

		// Assert
		databaseWriteCoordinatorMock.Verify(x => x.ExecuteAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Never);
		hashUpdatorMock.Verify(x => x.EnqueueHashUpdate(It.IsAny<long>()), Times.Never);
	}

	/// <summary>
	/// 属性未変更かつPreHashUpdatedTimeがnullの画像アイテムに対してDB更新を行わずハッシュ更新のみキューへ追加することを検証します。
	/// </summary>
	[Fact]
	public async Task UpdateFileInfo_PreHashNullWithUnchangedAttributes_ShouldQueueHashWithoutDbUpdate() {
		// Arrange
		var dbFactory = this.CreateInMemoryDbFactory();
		var hashUpdatorMock = new Mock<IFileHashUpdatorService>();
		var mediaTypeServiceMock = new Mock<IMediaItemTypeService>();
		var providerMock = new Mock<IMediaItemTypeProvider>();
		var databaseWriteCoordinatorMock = CreateDatabaseWriteCoordinatorMock();

		var now = new DateTime(2026, 11, 21, 10, 0, 0, DateTimeKind.Local);
		var filePath = "C:\\test\\no_prehash.jpg";

		await using (var db = await dbFactory.CreateDbContextAsync()) {
			db.MediaItems.Add(new MediaItem {
				MediaItemId = 5,
				MediaType = MediaType.Image,
				FilePath = filePath,
				IsExists = true,
				FileSize = 1000,
				CreationTime = now,
				ModifiedTime = now,
				LastAccessTime = now,
				PreHashUpdatedTime = null,
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

		var service = new FileStatusUpdatorService(dbFactory, hashUpdatorMock.Object, mediaTypeServiceMock.Object, databaseWriteCoordinatorMock.Object);

		// Act
		await service.UpdateFileInfo();

		// Assert
		databaseWriteCoordinatorMock.Verify(x => x.ExecuteAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Never);
		hashUpdatorMock.Verify(x => x.EnqueueHashUpdate(5L), Times.Once);
	}

	/// <summary>
	/// LastAccessTimeのみが変化した場合にDBのみ更新し、ハッシュ更新を行わないことを検証します。
	/// </summary>
	[Fact]
	public async Task UpdateFileInfo_LastAccessTimeOnlyChanged_ShouldUpdateDbWithoutHashUpdate() {
		// Arrange
		var dbFactory = this.CreateInMemoryDbFactory();
		var hashUpdatorMock = new Mock<IFileHashUpdatorService>();
		var mediaTypeServiceMock = new Mock<IMediaItemTypeService>();
		var providerMock = new Mock<IMediaItemTypeProvider>();

		var now = new DateTime(2026, 11, 21, 10, 0, 0, DateTimeKind.Local);
		var newLastAccessTime = now.AddHours(2);
		var filePath = "C:\\test\\accessed.jpg";

		await using (var db = await dbFactory.CreateDbContextAsync()) {
			db.MediaItems.Add(new MediaItem {
				MediaItemId = 6,
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
			lastAccessTime: newLastAccessTime
		));
		mediaTypeServiceMock.Setup(x => x.GetMediaItemTypeProvider(MediaType.Image)).Returns(providerMock.Object);

		var service = new FileStatusUpdatorService(dbFactory, hashUpdatorMock.Object, mediaTypeServiceMock.Object, CreateDatabaseWriteCoordinator());

		// Act
		await service.UpdateFileInfo();

		// Assert
		hashUpdatorMock.Verify(x => x.EnqueueHashUpdate(It.IsAny<long>()), Times.Never);

		await using (var db = await dbFactory.CreateDbContextAsync()) {
			var item = await db.MediaItems.FindAsync(6L);
			item!.LastAccessTime.ShouldBe(newLastAccessTime);
		}
	}

	/// <summary>
	/// スキャン中に別処理でリネームされたアイテムについて、旧パスの非存在検知で誤ってIsExists=falseへ上書きされないことを検証します。
	/// </summary>
	[Fact]
	public async Task UpdateFileInfo_FileRenamedDuringScan_ShouldNotOverwriteRenamedItemToMissing() {
		// Arrange
		var dbFactory = this.CreateInMemoryDbFactory();
		var hashUpdatorMock = new Mock<IFileHashUpdatorService>();
		var mediaTypeServiceMock = new Mock<IMediaItemTypeService>();
		var providerMock = new Mock<IMediaItemTypeProvider>();

		var now = new DateTime(2026, 11, 21, 10, 0, 0, DateTimeKind.Local);
		var oldPath = "C:\\test\\old_name.jpg";
		var newPath = "C:\\test\\new_name.jpg";

		await using (var db = await dbFactory.CreateDbContextAsync()) {
			db.MediaItems.Add(new MediaItem {
				MediaItemId = 7,
				MediaType = MediaType.Image,
				FilePath = oldPath,
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

		providerMock.Setup(x => x.GetPathStatus(oldPath)).Returns(() => {
			using var concurrentDb = dbFactory.CreateDbContextAsync().GetAwaiter().GetResult();
			var target = concurrentDb.MediaItems.Find(7L);
			target!.FilePath = newPath;
			concurrentDb.SaveChanges();

			return new MediaItemPathStatus(
				exists: false,
				fileSize: 0,
				creationTime: default,
				modifiedTime: default,
				lastAccessTime: default
			);
		});
		mediaTypeServiceMock.Setup(x => x.GetMediaItemTypeProvider(MediaType.Image)).Returns(providerMock.Object);

		var service = new FileStatusUpdatorService(dbFactory, hashUpdatorMock.Object, mediaTypeServiceMock.Object, CreateDatabaseWriteCoordinator());

		// Act
		await service.UpdateFileInfo();

		// Assert
		await using (var db = await dbFactory.CreateDbContextAsync()) {
			var item = await db.MediaItems.FindAsync(7L);
			item!.FilePath.ShouldBe(newPath);
			item.IsExists.ShouldBeTrue();
		}
	}

	/// <summary>
	/// スキャン中に別処理でリネーム・更新されたアイテムについて、旧パスの情報でメタデータが誤上書きされないことを検証します。
	/// </summary>
	[Fact]
	public async Task UpdateFileInfo_FileRenamedDuringScan_ShouldNotOverwriteRenamedItemMetadata() {
		// Arrange
		var dbFactory = this.CreateInMemoryDbFactory();
		var hashUpdatorMock = new Mock<IFileHashUpdatorService>();
		var mediaTypeServiceMock = new Mock<IMediaItemTypeService>();
		var providerMock = new Mock<IMediaItemTypeProvider>();

		var oldTime = new DateTime(2026, 11, 20, 10, 0, 0, DateTimeKind.Local);
		var newTime = new DateTime(2026, 11, 21, 10, 0, 0, DateTimeKind.Local);
		var oldPath = "C:\\test\\old_meta.jpg";
		var newPath = "C:\\test\\new_meta.jpg";

		await using (var db = await dbFactory.CreateDbContextAsync()) {
			db.MediaItems.Add(new MediaItem {
				MediaItemId = 8,
				MediaType = MediaType.Image,
				FilePath = oldPath,
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

		providerMock.Setup(x => x.GetPathStatus(oldPath)).Returns(() => {
			using var concurrentDb = dbFactory.CreateDbContextAsync().GetAwaiter().GetResult();
			var target = concurrentDb.MediaItems.Find(8L);
			target!.FilePath = newPath;
			target.FileSize = 9999;
			concurrentDb.SaveChanges();

			return new MediaItemPathStatus(
				exists: true,
				fileSize: 2000,
				creationTime: oldTime,
				modifiedTime: newTime,
				lastAccessTime: newTime
			);
		});
		mediaTypeServiceMock.Setup(x => x.GetMediaItemTypeProvider(MediaType.Image)).Returns(providerMock.Object);

		var service = new FileStatusUpdatorService(dbFactory, hashUpdatorMock.Object, mediaTypeServiceMock.Object, CreateDatabaseWriteCoordinator());

		// Act
		await service.UpdateFileInfo();

		// Assert
		await using (var db = await dbFactory.CreateDbContextAsync()) {
			var item = await db.MediaItems.FindAsync(8L);
			item!.FilePath.ShouldBe(newPath);
			item.FileSize.ShouldBe(9999);
		}
	}

	/// <summary>
	/// 複数の削除ファイルとリネーム済みファイルが混在する場合、パス未変更の削除アイテムのみがチャンク内で一括更新されることを検証します。
	/// </summary>
	[Fact]
	public async Task UpdateFileInfo_MultipleFilesDeletedWithConcurrentRename_ShouldBulkUpdateOnlyUnchangedMissingItems() {
		// Arrange
		var dbFactory = this.CreateInMemoryDbFactory();
		var hashUpdatorMock = new Mock<IFileHashUpdatorService>();
		var mediaTypeServiceMock = new Mock<IMediaItemTypeService>();
		var providerMock = new Mock<IMediaItemTypeProvider>();
		var databaseWriteCoordinatorMock = CreateDatabaseWriteCoordinatorMock();

		var now = new DateTime(2026, 11, 21, 10, 0, 0, DateTimeKind.Local);
		var deletedPath1 = "C:\\test\\missing1.jpg";
		var deletedPath2 = "C:\\test\\missing2.jpg";
		var renamedOldPath = "C:\\test\\renamed_old.jpg";
		var renamedNewPath = "C:\\test\\renamed_new.jpg";

		await using (var db = await dbFactory.CreateDbContextAsync()) {
			db.MediaItems.AddRange(
				new MediaItem {
					MediaItemId = 10,
					MediaType = MediaType.Image,
					FilePath = deletedPath1,
					IsExists = true,
					FileSize = 1000,
					CreationTime = now,
					ModifiedTime = now,
					LastAccessTime = now,
					PreHashUpdatedTime = now,
					DirectoryPath = "C:\\test",
					Description = "",
					IsUnderFolderGroup = false
				},
				new MediaItem {
					MediaItemId = 11,
					MediaType = MediaType.Image,
					FilePath = deletedPath2,
					IsExists = true,
					FileSize = 1000,
					CreationTime = now,
					ModifiedTime = now,
					LastAccessTime = now,
					PreHashUpdatedTime = now,
					DirectoryPath = "C:\\test",
					Description = "",
					IsUnderFolderGroup = false
				},
				new MediaItem {
					MediaItemId = 12,
					MediaType = MediaType.Image,
					FilePath = renamedOldPath,
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

		var missingStatus = new MediaItemPathStatus(
			exists: false,
			fileSize: 0,
			creationTime: default,
			modifiedTime: default,
			lastAccessTime: default
		);
		providerMock.Setup(x => x.GetPathStatus(deletedPath1)).Returns(missingStatus);
		providerMock.Setup(x => x.GetPathStatus(deletedPath2)).Returns(missingStatus);
		providerMock.Setup(x => x.GetPathStatus(renamedOldPath)).Returns(() => {
			using var concurrentDb = dbFactory.CreateDbContextAsync().GetAwaiter().GetResult();
			var target = concurrentDb.MediaItems.Find(12L);
			target!.FilePath = renamedNewPath;
			concurrentDb.SaveChanges();

			return missingStatus;
		});
		mediaTypeServiceMock.Setup(x => x.GetMediaItemTypeProvider(MediaType.Image)).Returns(providerMock.Object);

		var service = new FileStatusUpdatorService(dbFactory, hashUpdatorMock.Object, mediaTypeServiceMock.Object, databaseWriteCoordinatorMock.Object);

		// Act
		await service.UpdateFileInfo();

		// Assert
		databaseWriteCoordinatorMock.Verify(x => x.ExecuteAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Once);

		await using (var db = await dbFactory.CreateDbContextAsync()) {
			var item10 = await db.MediaItems.FindAsync(10L);
			var item11 = await db.MediaItems.FindAsync(11L);
			var item12 = await db.MediaItems.FindAsync(12L);

			item10!.IsExists.ShouldBeFalse();
			item11!.IsExists.ShouldBeFalse();
			item12!.IsExists.ShouldBeTrue();
			item12.FilePath.ShouldBe(renamedNewPath);
		}
	}

	/// <summary>
	/// 非存在だったファイルが再出現した場合、ハッシュ更新キューへ投入される前にDBのIsExists=trueが反映済みであることを検証します。
	/// </summary>
	[Fact]
	public async Task UpdateFileInfo_MissingFileReappears_ShouldUpdateDbBeforeEnqueuingHashUpdate() {
		// Arrange
		var dbFactory = this.CreateInMemoryDbFactory();
		var hashUpdatorMock = new Mock<IFileHashUpdatorService>();
		var mediaTypeServiceMock = new Mock<IMediaItemTypeService>();
		var providerMock = new Mock<IMediaItemTypeProvider>();

		var now = new DateTime(2026, 11, 21, 10, 0, 0, DateTimeKind.Local);
		var filePath = "C:\\test\\reappeared.jpg";

		await using (var db = await dbFactory.CreateDbContextAsync()) {
			db.MediaItems.Add(new MediaItem {
				MediaItemId = 13,
				MediaType = MediaType.Image,
				FilePath = filePath,
				IsExists = false,
				FileSize = 1000,
				CreationTime = now,
				ModifiedTime = now,
				LastAccessTime = now,
				PreHashUpdatedTime = null,
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

		var isExistsWhenEnqueued = false;
		hashUpdatorMock
			.Setup(x => x.EnqueueHashUpdate(13L))
			.Callback(() => {
				using var checkDb = dbFactory.CreateDbContextAsync().GetAwaiter().GetResult();
				var item = checkDb.MediaItems.Find(13L);
				isExistsWhenEnqueued = item?.IsExists == true;
			});

		var service = new FileStatusUpdatorService(dbFactory, hashUpdatorMock.Object, mediaTypeServiceMock.Object, CreateDatabaseWriteCoordinator());

		// Act
		await service.UpdateFileInfo();

		// Assert
		hashUpdatorMock.Verify(x => x.EnqueueHashUpdate(13L), Times.Once);
		isExistsWhenEnqueued.ShouldBeTrue();
	}
}