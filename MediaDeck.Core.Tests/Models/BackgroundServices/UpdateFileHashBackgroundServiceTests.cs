using System.Diagnostics;
using System.Reflection;

using MediaDeck.Core.Models.BackgroundServices;
using MediaDeck.Database;
using MediaDeck.Database.Tables;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Moq;
using Shouldly;

namespace MediaDeck.Core.Tests.Models.BackgroundServices;

public class UpdateFileHashBackgroundServiceTests : IDisposable {
	private readonly string _tempDir;
	private readonly Microsoft.Data.Sqlite.SqliteConnection _connection;
	private readonly DbContextOptions<MediaDeckDbContext> _options;

	public UpdateFileHashBackgroundServiceTests() {
		this._tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(this._tempDir);

		this._connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
		this._connection.Open();

		this._options = new DbContextOptionsBuilder<MediaDeckDbContext>()
			.UseSqlite(this._connection)
			.Options;

		using (var context = new MediaDeckDbContext(this._options)) {
			context.Database.EnsureCreated();
		}
	}

	public void Dispose() {
		if (Directory.Exists(this._tempDir)) {
			Directory.Delete(this._tempDir, true);
		}
		this._connection.Close();
		this._connection.Dispose();
	}

	private Mock<IDbContextFactory<MediaDeckDbContext>> CreateDbFactoryMock() {
		var mock = new Mock<IDbContextFactory<MediaDeckDbContext>>();
		mock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
			.Returns(() => {
				var context = new MediaDeckDbContext(this._options);
				return Task.FromResult(context);
			});
		return mock;
	}

	/// <summary>
	/// EnqueueHashUpdateが呼び出された際、キューにアイテムが追加され、TargetCountが増加することを確認する。
	/// </summary>
	[Fact]
	public void EnqueueHashUpdate_ShouldAddQueueAndIncrementTargetCount() {
		// Arrange
		var dbFactoryMock = this.CreateDbFactoryMock();
		var loggerMock = new Mock<ILogger<UpdateFileHashBackgroundService>>();
		var service = new UpdateFileHashBackgroundService(dbFactoryMock.Object, loggerMock.Object);

		// Act
		service.EnqueueHashUpdate(1L);

		// Assert
		service.TargetCount.Value.ShouldBe(1);
	}

	/// <summary>
	/// EnqueueHashUpdateRangeが呼び出された際、キューに複数のアイテムが追加され、TargetCountが正しく増加することを確認する。
	/// </summary>
	[Fact]
	public void EnqueueHashUpdateRange_ShouldAddQueueAndIncrementTargetCount() {
		// Arrange
		var dbFactoryMock = this.CreateDbFactoryMock();
		var loggerMock = new Mock<ILogger<UpdateFileHashBackgroundService>>();
		var service = new UpdateFileHashBackgroundService(dbFactoryMock.Object, loggerMock.Object);
		var ids = new List<long> { 1L, 2L, 3L };

		// Act
		service.EnqueueHashUpdateRange(ids);

		// Assert
		service.TargetCount.Value.ShouldBe(3);
	}

	/// <summary>
	/// PreHash更新キューが空の場合、重複PreHashのFullHashチェックが呼び出されることを確認する。
	/// </summary>
	[Fact]
	public async Task CheckAndEnqueueFullHashUpdatesAsync_WhenHashUpdateQueueIsEmpty_ShouldEnqueueFullHash() {
		// Arrange
		using (var context = new MediaDeckDbContext(this._options)) {
			context.MediaFiles.Add(new MediaFile { FilePath = "dummy1.jpg", DirectoryPath = "dir", IsExists = true, PreHash = "samehash", Description = string.Empty });
			context.MediaFiles.Add(new MediaFile { FilePath = "dummy2.jpg", DirectoryPath = "dir", IsExists = true, PreHash = "samehash", Description = string.Empty });
			await context.SaveChangesAsync();
		}

		var dbFactoryMock = this.CreateDbFactoryMock();
		var loggerMock = new Mock<ILogger<UpdateFileHashBackgroundService>>();
		var service = new UpdateFileHashBackgroundService(dbFactoryMock.Object, loggerMock.Object);

		// Act
		await service.CheckAndEnqueueFullHashUpdatesAsync();

		// Assert
		service.FullHashUpdateQueue.Count.ShouldBe(2);
		service.FullHashTargetCount.Value.ShouldBe(2);
	}

	/// <summary>
	/// PreHash更新キューが空ではない場合、重複PreHashのFullHashチェックが呼び出されないことを確認する。
	/// </summary>
	[Fact]
	public async Task CheckAndEnqueueFullHashUpdatesAsync_WhenHashUpdateQueueIsNotEmpty_ShouldNotEnqueueFullHash() {
		// Arrange
		var dbFactoryMock = this.CreateDbFactoryMock();
		var loggerMock = new Mock<ILogger<UpdateFileHashBackgroundService>>();
		var service = new UpdateFileHashBackgroundService(dbFactoryMock.Object, loggerMock.Object);

		service.HashUpdateQueue.Enqueue(1L);

		// Act
		await service.CheckAndEnqueueFullHashUpdatesAsync();

		// Assert
		service.FullHashUpdateQueue.Count.ShouldBe(0);
	}

	/// <summary>
	/// キュー内のメディアファイルのPreHashが正しく更新されることを確認する。
	/// </summary>
	[Fact]
	public async Task UpdateHashAsync_ShouldUpdatePreHash() {
		// Arrange
		var tempFile = Path.Combine(this._tempDir, "test.txt");
		await File.WriteAllTextAsync(tempFile, "dummy content");

		long mediaFileId;
		using (var context = new MediaDeckDbContext(this._options)) {
			var mediaFile = new MediaFile { FilePath = tempFile, DirectoryPath = "dir", IsExists = true, Description = string.Empty };
			context.MediaFiles.Add(mediaFile);
			await context.SaveChangesAsync();
			mediaFileId = mediaFile.MediaFileId;
		}

		var mockFactory = this.CreateDbFactoryMock();
		var loggerMock = new Mock<ILogger<UpdateFileHashBackgroundService>>();
		var service = new UpdateFileHashBackgroundService(mockFactory.Object, loggerMock.Object);

		var method = typeof(UpdateFileHashBackgroundService).GetMethod("UpdateHashAsync", BindingFlags.NonPublic | BindingFlags.Instance);
		method.ShouldNotBeNull();

		// Act
		service.HashUpdateQueue.Enqueue(mediaFileId);
		var task = (Task)method.Invoke(service, null)!;
		await task;

		// Assert
		using (var context = new MediaDeckDbContext(this._options)) {
			var updatedMediaFile = await context.MediaFiles.FindAsync(mediaFileId);
			updatedMediaFile.ShouldNotBeNull();
			updatedMediaFile.PreHash.ShouldNotBeNullOrEmpty();
			updatedMediaFile.PreHashUpdatedTime.ShouldNotBeNull();
		}
	}

	/// <summary>
	/// メディアファイルが存在しない（IsExists == false）場合、処理をスキップすることを確認する。
	/// </summary>
	[Fact]
	public async Task UpdateHashAsync_WhenMediaFileNotExists_ShouldContinue() {
		// Arrange
		long mediaFileId;
		using (var context = new MediaDeckDbContext(this._options)) {
			var mediaFile = new MediaFile { FilePath = "dummy.txt", DirectoryPath = "dir", IsExists = false, Description = string.Empty };
			context.MediaFiles.Add(mediaFile);
			await context.SaveChangesAsync();
			mediaFileId = mediaFile.MediaFileId;
		}

		var mockFactory = this.CreateDbFactoryMock();
		var loggerMock = new Mock<ILogger<UpdateFileHashBackgroundService>>();
		var service = new UpdateFileHashBackgroundService(mockFactory.Object, loggerMock.Object);

		var method = typeof(UpdateFileHashBackgroundService).GetMethod("UpdateHashAsync", BindingFlags.NonPublic | BindingFlags.Instance);

		// Act
		service.HashUpdateQueue.Enqueue(mediaFileId);
		var task = (Task)method!.Invoke(service, null)!;
		await task;

		// Assert
		using (var context = new MediaDeckDbContext(this._options)) {
			var mediaFile = await context.MediaFiles.FindAsync(mediaFileId);
			mediaFile.ShouldNotBeNull();
			mediaFile.PreHash.ShouldBeNull(); // 更新されていないこと
		}
	}

	/// <summary>
	/// ファイル読み込みで例外が発生した場合、エラーログを出力して処理を続行することを確認する。
	/// </summary>
	[Fact]
	public async Task UpdateHashAsync_WhenFileDoesNotExist_ShouldLogErrorAndContinue() {
		// Arrange
		long mediaFileId;
		using (var context = new MediaDeckDbContext(this._options)) {
			var mediaFile = new MediaFile { FilePath = "not_found.txt", DirectoryPath = "dir", IsExists = true, Description = string.Empty };
			context.MediaFiles.Add(mediaFile);
			await context.SaveChangesAsync();
			mediaFileId = mediaFile.MediaFileId;
		}

		var mockFactory = this.CreateDbFactoryMock();
		var loggerMock = new Mock<ILogger<UpdateFileHashBackgroundService>>();
		var service = new UpdateFileHashBackgroundService(mockFactory.Object, loggerMock.Object);

		var method = typeof(UpdateFileHashBackgroundService).GetMethod("UpdateHashAsync", BindingFlags.NonPublic | BindingFlags.Instance);

		// Act
		service.HashUpdateQueue.Enqueue(mediaFileId);
		var task = (Task)method!.Invoke(service, null)!;
		await task;

		// Assert
		loggerMock.Verify(
			x => x.Log(
				LogLevel.Error,
				It.IsAny<EventId>(),
				It.Is<It.IsAnyType>((v, t) => true),
				It.IsAny<Exception>(),
				It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
			Times.Once);
	}

	/// <summary>
	/// キュー内のメディアファイルのFullHashが正しく更新されることを確認する。
	/// </summary>
	[Fact]
	public async Task UpdateFullHashAsync_ShouldUpdateFullHash() {
		// Arrange
		var tempFile = Path.Combine(this._tempDir, "test_full.txt");
		await File.WriteAllTextAsync(tempFile, "dummy full content");

		long mediaFileId;
		using (var context = new MediaDeckDbContext(this._options)) {
			var mediaFile = new MediaFile { FilePath = tempFile, DirectoryPath = "dir", IsExists = true, Description = string.Empty };
			context.MediaFiles.Add(mediaFile);
			await context.SaveChangesAsync();
			mediaFileId = mediaFile.MediaFileId;
		}

		var mockFactory = this.CreateDbFactoryMock();
		var loggerMock = new Mock<ILogger<UpdateFileHashBackgroundService>>();
		var service = new UpdateFileHashBackgroundService(mockFactory.Object, loggerMock.Object);

		var method = typeof(UpdateFileHashBackgroundService).GetMethod("UpdateFullHashAsync", BindingFlags.NonPublic | BindingFlags.Instance);
		method.ShouldNotBeNull();

		// Act
		service.FullHashUpdateQueue.Enqueue(mediaFileId);
		var task = (Task)method.Invoke(service, null)!;
		await task;

		// Assert
		using (var context = new MediaDeckDbContext(this._options)) {
			var updatedMediaFile = await context.MediaFiles.FindAsync(mediaFileId);
			updatedMediaFile.ShouldNotBeNull();
			updatedMediaFile.FullHash.ShouldNotBeNullOrEmpty();
			updatedMediaFile.FullHashUpdatedTime.ShouldNotBeNull();
		}
	}

	/// <summary>
	/// PreHashが重複しているファイルのFullHash更新がキューに追加されることを確認する。
	/// </summary>
	[Fact]
	public async Task EnqueueFullHashUpdatesForDuplicatePreHashAsync_ShouldEnqueueForDuplicatePreHash() {
		// Arrange
		using (var context = new MediaDeckDbContext(this._options)) {
			var mediaFile1 = new MediaFile { FilePath = "dummy1.txt", DirectoryPath = "dir", IsExists = true, PreHash = "dup_hash", Description = string.Empty, PreHashUpdatedTime = DateTime.Now };
			var mediaFile2 = new MediaFile { FilePath = "dummy2.txt", DirectoryPath = "dir", IsExists = true, PreHash = "dup_hash", Description = string.Empty, PreHashUpdatedTime = DateTime.Now };
			context.MediaFiles.Add(mediaFile1);
			context.MediaFiles.Add(mediaFile2);
			await context.SaveChangesAsync();
		}

		var mockFactory = this.CreateDbFactoryMock();
		var loggerMock = new Mock<ILogger<UpdateFileHashBackgroundService>>();
		var service = new UpdateFileHashBackgroundService(mockFactory.Object, loggerMock.Object);

		var method = typeof(UpdateFileHashBackgroundService).GetMethod("EnqueueFullHashUpdatesForDuplicatePreHashAsync", BindingFlags.NonPublic | BindingFlags.Instance);
		method.ShouldNotBeNull();

		// Act
		var task = (Task)method.Invoke(service, null)!;
		await task;

		// Assert
		service.FullHashUpdateQueue.Count.ShouldBeGreaterThanOrEqualTo(2);
		service.FullHashTargetCount.Value.ShouldBe(2);
	}

	/// <summary>
	/// PreHashが重複していないメディアファイルのFullHashがクリアされることを確認するパフォーマンス・検証テスト。
	/// </summary>
	[Fact]
	public async Task ClearFullHashForNonDuplicatePreHashAsync_PerformanceTest() {
		// Arrange
		using (var context = new MediaDeckDbContext(this._options)) {
			var mediaFiles = new List<MediaFile>();
			for (int i = 0; i < 100; i++) {
				mediaFiles.Add(new MediaFile {
					FilePath = $"path{i}.jpg",
					DirectoryPath = "dir",
					Description = string.Empty,
					IsExists = true,
					PreHash = $"pre{i}",
					FullHash = $"full{i}",
					FullHashUpdatedTime = DateTime.Now
				});
			}
			context.MediaFiles.AddRange(mediaFiles);
			await context.SaveChangesAsync();
		}

		var mockFactory = this.CreateDbFactoryMock();
		var loggerMock = new Mock<ILogger<UpdateFileHashBackgroundService>>();
		var service = new UpdateFileHashBackgroundService(mockFactory.Object, loggerMock.Object);

		var method = typeof(UpdateFileHashBackgroundService).GetMethod("ClearFullHashForNonDuplicatePreHashAsync", BindingFlags.NonPublic | BindingFlags.Instance);
		method.ShouldNotBeNull();

		// Act
		var sw = Stopwatch.StartNew();
		var task = (Task)method.Invoke(service, null)!;
		await task;
		sw.Stop();

		// Assert
		using (var context = new MediaDeckDbContext(this._options)) {
			var remainingFullHashes = await context.MediaFiles.CountAsync(m => m.FullHash != null);
			remainingFullHashes.ShouldBe(0);
		}

		Console.WriteLine($"Execution time for 100 records: {sw.ElapsedMilliseconds}ms");
	}
}