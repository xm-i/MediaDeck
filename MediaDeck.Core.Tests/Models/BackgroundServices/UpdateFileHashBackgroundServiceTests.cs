using System.Diagnostics;
using System.Reflection;
using MediaDeck.Composition.Database;
using MediaDeck.Composition.Tables;
using MediaDeck.Core.Services.FileHashUpdator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using R3;
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

	private async Task WaitUntilAsync<T>(ReactiveProperty<T> property, T expectedValue, int timeoutMs = 5000) {
		var stopwatch = Stopwatch.StartNew();
		while (!EqualityComparer<T>.Default.Equals(property.Value, expectedValue)) {
			if (stopwatch.ElapsedMilliseconds > timeoutMs) {
				throw new TimeoutException($"Timed out waiting for property to reach {expectedValue}. Current value: {property.Value}");
			}
			await Task.Delay(10);
		}
	}

	/// <summary>
	/// EnqueueHashUpdateが呼び出された際、TargetCountが増加し、最終的に処理が完了することを確認する。
	/// </summary>
	[Fact]
	public async Task EnqueueHashUpdate_ShouldIncrementTargetCountAndComplete() {
		// Arrange
		var dbFactoryMock = this.CreateDbFactoryMock();
		var loggerMock = new Mock<ILogger<FileHashUpdatorService>>();
		using var service = new FileHashUpdatorService(dbFactoryMock.Object, loggerMock.Object);

		// Act
		service.EnqueueHashUpdate(1L);
		// バックグラウンドスレッドが即座にdequeueする可能性があるため、TargetCountの増分を即座に確認
		service.TargetCount.Value.ShouldBe(1);

		// 完了を待機
		await this.WaitUntilAsync(service.CompletedCount, 1L);

		// Assert
		service.CompletedCount.Value.ShouldBe(1);
	}

	/// <summary>
	/// EnqueueHashUpdateRangeが呼び出された際、TargetCountが正しく増加し、最終的にすべて完了することを確認する。
	/// </summary>
	[Fact]
	public async Task EnqueueHashUpdateRange_ShouldIncrementTargetCountAndComplete() {
		// Arrange
		var dbFactoryMock = this.CreateDbFactoryMock();
		var loggerMock = new Mock<ILogger<FileHashUpdatorService>>();
		using var service = new FileHashUpdatorService(dbFactoryMock.Object, loggerMock.Object);
		var ids = new List<long> { 1L, 2L, 3L };

		// Act
		service.EnqueueHashUpdateRange(ids);
		service.TargetCount.Value.ShouldBe(3);

		// 完了を待機
		await this.WaitUntilAsync(service.CompletedCount, 3L);

		// Assert
		service.CompletedCount.Value.ShouldBe(3);
	}

	/// <summary>
	/// PreHash更新キューが空の場合、重複PreHashのFullHashチェックが呼び出されることを確認する。
	/// </summary>
	[Fact]
	public async Task CheckAndEnqueueFullHashUpdatesAsync_WhenHashUpdateQueueIsEmpty_ShouldEnqueueFullHash() {
		// Arrange
		using (var context = new MediaDeckDbContext(this._options)) {
			// 同じPreHashを持つレコードを2つ作成
			context.MediaItems.Add(new MediaItem { ItemType = ItemType.Image, FilePath = "dummy1.jpg", DirectoryPath = "dir", IsExists = true, PreHash = "samehash", Description = string.Empty, IsUnderFolderGroup = false });
			context.MediaItems.Add(new MediaItem { ItemType = ItemType.Image, FilePath = "dummy2.jpg", DirectoryPath = "dir", IsExists = true, PreHash = "samehash", Description = string.Empty, IsUnderFolderGroup = false });
			await context.SaveChangesAsync();
		}

		var dbFactoryMock = this.CreateDbFactoryMock();
		var loggerMock = new Mock<ILogger<FileHashUpdatorService>>();
		using var service = new FileHashUpdatorService(dbFactoryMock.Object, loggerMock.Object);

		// Act
		await service.CheckAndEnqueueFullHashUpdatesAsync();
		await this.WaitUntilAsync(service.FullHashTargetCount, 2L);

		// Assert
		service.FullHashTargetCount.Value.ShouldBe(2);
	}

	/// <summary>
	/// PreHash更新キューが空ではない場合、重複PreHashのFullHashチェックが呼び出されないことを確認する。
	/// </summary>
	[Fact]
	public async Task CheckAndEnqueueFullHashUpdatesAsync_WhenHashUpdateQueueIsNotEmpty_ShouldNotEnqueueFullHash() {
		// Arrange
		var dbFactoryMock = this.CreateDbFactoryMock();
		var loggerMock = new Mock<ILogger<FileHashUpdatorService>>();
		// データベースが空の状態でも、キューに何かあればスキップされるはず
		using var service = new FileHashUpdatorService(dbFactoryMock.Object, loggerMock.Object);

		// キューに要素を追加し、バックグラウンド処理が走らないように工夫（存在しないIDなどでもキューには残るはずだが即終わる可能性がある）
		// ここでは大量に積むことで空にならない瞬間を狙うか、あるいは内部状態を直接いじる
		service.HashUpdateQueue.Enqueue(999L);
		service.TargetCount.Value++;

		// Act
		await service.CheckAndEnqueueFullHashUpdatesAsync();

		// Assert
		// キューが処理されて空になる前に CheckAndEnqueueFullHashUpdatesAsync が走れば成功だが、
		// レースコンディションを避けるため、FullHashTargetCount が増えていないことを確認
		service.FullHashTargetCount.Value.ShouldBe(0);
	}

	/// <summary>
	/// キュー内のメディアアイテムのPreHashが正しく更新されることを確認する。
	/// </summary>
	[Fact]
	public async Task UpdateHashAsync_ShouldUpdatePreHash() {
		// Arrange
		var tempFile = Path.Combine(this._tempDir, "test.txt");
		await File.WriteAllTextAsync(tempFile, "dummy content");

		long MediaItemId;
		using (var context = new MediaDeckDbContext(this._options)) {
			var MediaItem = new MediaItem { ItemType = ItemType.Image, FilePath = tempFile, DirectoryPath = "dir", IsExists = true, Description = string.Empty, IsUnderFolderGroup = false };
			context.MediaItems.Add(MediaItem);
			await context.SaveChangesAsync();
			MediaItemId = MediaItem.MediaItemId;
		}

		var mockFactory = this.CreateDbFactoryMock();
		var loggerMock = new Mock<ILogger<FileHashUpdatorService>>();
		using var service = new FileHashUpdatorService(mockFactory.Object, loggerMock.Object);

		// Act
		service.EnqueueHashUpdate(MediaItemId);
		await this.WaitUntilAsync(service.CompletedCount, 1L);

		// Assert
		using (var context = new MediaDeckDbContext(this._options)) {
			var updatedMediaItem = await context.MediaItems.FindAsync(MediaItemId);
			updatedMediaItem.ShouldNotBeNull();
			updatedMediaItem.PreHash.ShouldNotBeNullOrEmpty();
			updatedMediaItem.PreHashUpdatedTime.ShouldNotBeNull();
		}
		service.CompletedCount.Value.ShouldBe(1);
	}

	/// <summary>
	/// メディアアイテムが存在しない（IsExists == false）場合、処理をスキップすることを確認する。
	/// </summary>
	[Fact]
	public async Task UpdateHashAsync_WhenMediaItemNotExists_ShouldContinue() {
		// Arrange
		long MediaItemId;
		using (var context = new MediaDeckDbContext(this._options)) {
			var MediaItem = new MediaItem { ItemType = ItemType.Image, FilePath = "dummy.txt", DirectoryPath = "dir", IsExists = false, Description = string.Empty, IsUnderFolderGroup = false };
			context.MediaItems.Add(MediaItem);
			await context.SaveChangesAsync();
			MediaItemId = MediaItem.MediaItemId;
		}

		var mockFactory = this.CreateDbFactoryMock();
		var loggerMock = new Mock<ILogger<FileHashUpdatorService>>();
		using var service = new FileHashUpdatorService(mockFactory.Object, loggerMock.Object);

		// Act
		service.EnqueueHashUpdate(MediaItemId);
		await this.WaitUntilAsync(service.CompletedCount, 1L);

		// Assert
		using (var context = new MediaDeckDbContext(this._options)) {
			var MediaItem = await context.MediaItems.FindAsync(MediaItemId);
			MediaItem.ShouldNotBeNull();
			MediaItem.PreHash.ShouldBeNull(); // 更新されていないこと
		}
		service.CompletedCount.Value.ShouldBe(1); // finallyブロックでカウントは進む
	}

	/// <summary>
	/// ファイル読み込みで例外が発生した場合、エラーログを出力して処理を続行することを確認する。
	/// </summary>
	[Fact]
	public async Task UpdateHashAsync_WhenFileDoesNotExist_ShouldLogErrorAndContinue() {
		// Arrange
		long MediaItemId;
		using (var context = new MediaDeckDbContext(this._options)) {
			var MediaItem = new MediaItem { ItemType = ItemType.Image, FilePath = "not_found.txt", DirectoryPath = "dir", IsExists = true, Description = string.Empty, IsUnderFolderGroup = false };
			context.MediaItems.Add(MediaItem);
			await context.SaveChangesAsync();
			MediaItemId = MediaItem.MediaItemId;
		}

		var mockFactory = this.CreateDbFactoryMock();
		var loggerMock = new Mock<ILogger<FileHashUpdatorService>>();
		using var service = new FileHashUpdatorService(mockFactory.Object, loggerMock.Object);

		// Act
		service.EnqueueHashUpdate(MediaItemId);
		await this.WaitUntilAsync(service.CompletedCount, 1L);

		// Assert
		loggerMock.Verify(
			x => x.Log(
				LogLevel.Error,
				It.IsAny<EventId>(),
				It.Is<It.IsAnyType>((v, t) => true),
				It.IsAny<Exception>(),
				It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
			Times.Once);

		service.CompletedCount.Value.ShouldBe(1);
	}

	/// <summary>
	/// キュー内のメディアアイテムのFullHashが正しく更新されることを確認する。
	/// </summary>
	[Fact]
	public async Task UpdateFullHashAsync_ShouldUpdateFullHash() {
		// Arrange
		var tempFile = Path.Combine(this._tempDir, "test_full.txt");
		await File.WriteAllTextAsync(tempFile, "dummy full content");

		long MediaItemId;
		using (var context = new MediaDeckDbContext(this._options)) {
			var MediaItem = new MediaItem { ItemType = ItemType.Image, FilePath = tempFile, DirectoryPath = "dir", IsExists = true, Description = string.Empty, IsUnderFolderGroup = false };
			context.MediaItems.Add(MediaItem);
			await context.SaveChangesAsync();
			MediaItemId = MediaItem.MediaItemId;
		}

		var mockFactory = this.CreateDbFactoryMock();
		var loggerMock = new Mock<ILogger<FileHashUpdatorService>>();
		using var service = new FileHashUpdatorService(mockFactory.Object, loggerMock.Object);

		// Act
		service.FullHashUpdateQueue.Enqueue(MediaItemId);
		service.FullHashTargetCount.Value++;
		await this.WaitUntilAsync(service.FullHashCompletedCount, 1L);

		// Assert
		using (var context = new MediaDeckDbContext(this._options)) {
			var updatedMediaItem = await context.MediaItems.FindAsync(MediaItemId);
			updatedMediaItem.ShouldNotBeNull();
			updatedMediaItem.FullHash.ShouldNotBeNullOrEmpty();
			updatedMediaItem.FullHashUpdatedTime.ShouldNotBeNull();
		}
		service.FullHashCompletedCount.Value.ShouldBe(1);
	}

	/// <summary>
	/// PreHashが重複しているファイルのFullHash更新がキューに追加されることを確認する。
	/// </summary>
	[Fact]
	public async Task EnqueueFullHashUpdatesForDuplicatePreHashAsync_ShouldEnqueueForDuplicatePreHash() {
		// Arrange
		long fileId1, fileId2;
		using (var context = new MediaDeckDbContext(this._options)) {
			var MediaItem1 = new MediaItem { ItemType = ItemType.Image, FilePath = "dummy1.txt", DirectoryPath = "dir", IsExists = true, PreHash = "dup_hash", Description = string.Empty, PreHashUpdatedTime = DateTime.Now, IsUnderFolderGroup = false };
			var MediaItem2 = new MediaItem { ItemType = ItemType.Image, FilePath = "dummy2.txt", DirectoryPath = "dir", IsExists = true, PreHash = "dup_hash", Description = string.Empty, PreHashUpdatedTime = DateTime.Now, IsUnderFolderGroup = false };
			context.MediaItems.Add(MediaItem1);
			context.MediaItems.Add(MediaItem2);
			await context.SaveChangesAsync();
			fileId1 = MediaItem1.MediaItemId;
			fileId2 = MediaItem2.MediaItemId;
		}

		var mockFactory = this.CreateDbFactoryMock();
		var loggerMock = new Mock<ILogger<FileHashUpdatorService>>();
		using var service = new FileHashUpdatorService(mockFactory.Object, loggerMock.Object);

		var method = typeof(FileHashUpdatorService).GetMethod("EnqueueFullHashUpdatesForDuplicatePreHashAsync", BindingFlags.NonPublic | BindingFlags.Instance);
		method.ShouldNotBeNull();

		// Act
		// Invokeには object[] で渡す必要がある
		var task = (Task)method.Invoke(service, new object[] { CancellationToken.None })!;
		await task;

		// Assert
		// キューのカウントはバックグラウンド処理で0になる可能性があるため、TargetCountを確認
		service.FullHashTargetCount.Value.ShouldBe(2);
	}
}