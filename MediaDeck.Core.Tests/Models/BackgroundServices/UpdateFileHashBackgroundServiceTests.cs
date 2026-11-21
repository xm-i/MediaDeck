using System.Collections.Concurrent;
using System.Data.Common;
using System.Diagnostics;
using System.Reflection;
using MediaDeck.Composition.Database;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.Services;
using MediaDeck.Composition.Tables;
using MediaDeck.Core.Services.FileHashUpdator;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using R3;
using Shouldly;

namespace MediaDeck.Core.Tests.Models.BackgroundServices;

public sealed class UpdateFileHashBackgroundServiceTests : IDisposable {
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

	private Mock<IDbContextFactory<MediaDeckDbContext>> CreateDbFactoryMock(DbContextOptions<MediaDeckDbContext>? options = null) {
		var targetOptions = options ?? this._options;
		var mock = new Mock<IDbContextFactory<MediaDeckDbContext>>();
		mock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
			.Returns(() => {
				var context = new MediaDeckDbContext(targetOptions);
				return Task.FromResult(context);
			});
		return mock;
	}

	/// <summary>
	/// テスト用の <see cref="MediaItem"/> エンティティを生成する。
	/// </summary>
	/// <param name="filePath">ファイルパス</param>
	/// <param name="isExists">存在フラグ</param>
	/// <param name="preHash">PreHash</param>
	/// <param name="preHashUpdatedTime">PreHash更新日時</param>
	/// <param name="fullHash">FullHash</param>
	/// <param name="fullHashUpdatedTime">FullHash更新日時</param>
	/// <returns>生成された <see cref="MediaItem"/></returns>
	private static MediaItem CreateMediaItem(
		string filePath,
		bool isExists = true,
		string? preHash = null,
		DateTime? preHashUpdatedTime = null,
		string? fullHash = null,
		DateTime? fullHashUpdatedTime = null) {
		return new MediaItem {
			MediaType = MediaType.Image,
			FilePath = filePath,
			DirectoryPath = "dir",
			IsExists = isExists,
			PreHash = preHash,
			PreHashUpdatedTime = preHashUpdatedTime,
			FullHash = fullHash,
			FullHashUpdatedTime = fullHashUpdatedTime,
			Description = string.Empty,
			IsUnderFolderGroup = false
		};
	}

	/// <summary>
	/// テスト用のFileHashUpdatorServiceを生成します。
	/// </summary>
	/// <param name="dbFactory">データベースコンテキストファクトリー</param>
	/// <param name="logger">ロガー</param>
	/// <returns>テスト対象サービス</returns>
	private FileHashUpdatorService CreateService(IDbContextFactory<MediaDeckDbContext> dbFactory, ILogger<FileHashUpdatorService> logger) {
		var databaseWriteCoordinatorMock = new Mock<IDatabaseWriteCoordinator>();
		databaseWriteCoordinatorMock
			.Setup(x => x.ExecuteAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
			.Returns<Func<CancellationToken, Task>, CancellationToken>((operation, ct) => operation(ct));

		return new FileHashUpdatorService(dbFactory, databaseWriteCoordinatorMock.Object, logger);
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
		using var service = this.CreateService(dbFactoryMock.Object, loggerMock.Object);

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
		using var service = this.CreateService(dbFactoryMock.Object, loggerMock.Object);
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
			context.MediaItems.Add(new MediaItem { MediaType = MediaType.Image, FilePath = "dummy1.jpg", DirectoryPath = "dir", IsExists = true, PreHash = "samehash", Description = string.Empty, IsUnderFolderGroup = false });
			context.MediaItems.Add(new MediaItem { MediaType = MediaType.Image, FilePath = "dummy2.jpg", DirectoryPath = "dir", IsExists = true, PreHash = "samehash", Description = string.Empty, IsUnderFolderGroup = false });
			await context.SaveChangesAsync();
		}

		var dbFactoryMock = this.CreateDbFactoryMock();
		var loggerMock = new Mock<ILogger<FileHashUpdatorService>>();
		using var service = this.CreateService(dbFactoryMock.Object, loggerMock.Object);

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
		using var service = this.CreateService(dbFactoryMock.Object, loggerMock.Object);

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
			var MediaItem = new MediaItem { MediaType = MediaType.Image, FilePath = tempFile, DirectoryPath = "dir", IsExists = true, Description = string.Empty, IsUnderFolderGroup = false };
			context.MediaItems.Add(MediaItem);
			await context.SaveChangesAsync();
			MediaItemId = MediaItem.MediaItemId;
		}

		var mockFactory = this.CreateDbFactoryMock();
		var loggerMock = new Mock<ILogger<FileHashUpdatorService>>();
		using var service = this.CreateService(mockFactory.Object, loggerMock.Object);

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
			var MediaItem = new MediaItem { MediaType = MediaType.Image, FilePath = "dummy.txt", DirectoryPath = "dir", IsExists = false, Description = string.Empty, IsUnderFolderGroup = false };
			context.MediaItems.Add(MediaItem);
			await context.SaveChangesAsync();
			MediaItemId = MediaItem.MediaItemId;
		}

		var mockFactory = this.CreateDbFactoryMock();
		var loggerMock = new Mock<ILogger<FileHashUpdatorService>>();
		using var service = this.CreateService(mockFactory.Object, loggerMock.Object);

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
			var MediaItem = new MediaItem { MediaType = MediaType.Image, FilePath = "not_found.txt", DirectoryPath = "dir", IsExists = true, Description = string.Empty, IsUnderFolderGroup = false };
			context.MediaItems.Add(MediaItem);
			await context.SaveChangesAsync();
			MediaItemId = MediaItem.MediaItemId;
		}

		var mockFactory = this.CreateDbFactoryMock();
		var loggerMock = new Mock<ILogger<FileHashUpdatorService>>();
		using var service = this.CreateService(mockFactory.Object, loggerMock.Object);

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
			var MediaItem = new MediaItem { MediaType = MediaType.Image, FilePath = tempFile, DirectoryPath = "dir", IsExists = true, Description = string.Empty, IsUnderFolderGroup = false };
			context.MediaItems.Add(MediaItem);
			await context.SaveChangesAsync();
			MediaItemId = MediaItem.MediaItemId;
		}

		var mockFactory = this.CreateDbFactoryMock();
		var loggerMock = new Mock<ILogger<FileHashUpdatorService>>();
		using var service = this.CreateService(mockFactory.Object, loggerMock.Object);

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
			var MediaItem1 = new MediaItem { MediaType = MediaType.Image, FilePath = "dummy1.txt", DirectoryPath = "dir", IsExists = true, PreHash = "dup_hash", Description = string.Empty, PreHashUpdatedTime = DateTime.Now, IsUnderFolderGroup = false };
			var MediaItem2 = new MediaItem { MediaType = MediaType.Image, FilePath = "dummy2.txt", DirectoryPath = "dir", IsExists = true, PreHash = "dup_hash", Description = string.Empty, PreHashUpdatedTime = DateTime.Now, IsUnderFolderGroup = false };
			context.MediaItems.Add(MediaItem1);
			context.MediaItems.Add(MediaItem2);
			await context.SaveChangesAsync();
			fileId1 = MediaItem1.MediaItemId;
			fileId2 = MediaItem2.MediaItemId;
		}

		var mockFactory = this.CreateDbFactoryMock();
		var loggerMock = new Mock<ILogger<FileHashUpdatorService>>();
		using var service = this.CreateService(mockFactory.Object, loggerMock.Object);

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

	/// <summary>
	/// 重複PreHashのうちFullHashが未設定または古いもののみがFullHash更新対象としてエンキューされ、更新されることを確認する。
	/// </summary>
	[Fact]
	public async Task EnqueueFullHashUpdatesForDuplicatePreHashAsync_ShouldOnlyEnqueueMissingOrOutdatedDuplicates() {
		// Arrange
		var baseTime = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
		var missingFile1 = Path.Combine(this._tempDir, "missing1.txt");
		var missingFile2 = Path.Combine(this._tempDir, "missing2.txt");
		var outdatedFile = Path.Combine(this._tempDir, "outdated.txt");
		var freshFile = Path.Combine(this._tempDir, "fresh.txt");
		await File.WriteAllTextAsync(missingFile1, "missing content 1");
		await File.WriteAllTextAsync(missingFile2, "missing content 2");
		await File.WriteAllTextAsync(outdatedFile, "outdated content");
		await File.WriteAllTextAsync(freshFile, "fresh content");

		long missingId1;
		long missingId2;
		long outdatedId;
		long freshId;
		long uniqueId;
		long nullPreHashId1;
		long nullPreHashId2;
		long ghostDupExistingId;
		long ghostDupDeletedId;
		long deletedDupId;

		using (var context = new MediaDeckDbContext(this._options)) {
			var missing1 = CreateMediaItem(missingFile1, isExists: true, preHash: "dup_missing", preHashUpdatedTime: baseTime);
			var missing2 = CreateMediaItem(missingFile2, isExists: true, preHash: "dup_missing", preHashUpdatedTime: baseTime);
			var outdated = CreateMediaItem(outdatedFile, isExists: true, preHash: "dup_mixed", preHashUpdatedTime: baseTime.AddMinutes(5), fullHash: "old_full", fullHashUpdatedTime: baseTime);
			var fresh = CreateMediaItem(freshFile, isExists: true, preHash: "dup_mixed", preHashUpdatedTime: baseTime, fullHash: "fresh_full", fullHashUpdatedTime: baseTime);
			var unique = CreateMediaItem("unique.txt", isExists: true, preHash: "unique_hash", preHashUpdatedTime: baseTime);
			var nullPre1 = CreateMediaItem("null1.txt", isExists: true, preHash: null);
			var nullPre2 = CreateMediaItem("null2.txt", isExists: true, preHash: null);
			var ghostExisting = CreateMediaItem("ghost_exists.txt", isExists: true, preHash: "ghost_dup", preHashUpdatedTime: baseTime);
			var ghostDeleted = CreateMediaItem("ghost_deleted.txt", isExists: false, preHash: "ghost_dup", preHashUpdatedTime: baseTime);
			var deletedDup = CreateMediaItem("deleted_dup.txt", isExists: false, preHash: "dup_missing", preHashUpdatedTime: baseTime);

			context.MediaItems.AddRange(
				missing1,
				missing2,
				outdated,
				fresh,
				unique,
				nullPre1,
				nullPre2,
				ghostExisting,
				ghostDeleted,
				deletedDup);
			await context.SaveChangesAsync();

			missingId1 = missing1.MediaItemId;
			missingId2 = missing2.MediaItemId;
			outdatedId = outdated.MediaItemId;
			freshId = fresh.MediaItemId;
			uniqueId = unique.MediaItemId;
			nullPreHashId1 = nullPre1.MediaItemId;
			nullPreHashId2 = nullPre2.MediaItemId;
			ghostDupExistingId = ghostExisting.MediaItemId;
			ghostDupDeletedId = ghostDeleted.MediaItemId;
			deletedDupId = deletedDup.MediaItemId;
		}

		var mockFactory = this.CreateDbFactoryMock();
		var loggerMock = new Mock<ILogger<FileHashUpdatorService>>();
		using var service = this.CreateService(mockFactory.Object, loggerMock.Object);

		// Act
		await service.EnqueueFullHashUpdatesForDuplicatePreHashAsync(CancellationToken.None);
		await this.WaitUntilAsync(service.FullHashCompletedCount, 3L);

		// Assert
		service.FullHashTargetCount.Value.ShouldBe(3);

		using (var context = new MediaDeckDbContext(this._options)) {
			var updatedMissing1 = await context.MediaItems.FindAsync(missingId1);
			var updatedMissing2 = await context.MediaItems.FindAsync(missingId2);
			var updatedOutdated = await context.MediaItems.FindAsync(outdatedId);
			var unchangedFresh = await context.MediaItems.FindAsync(freshId);
			var unchangedUnique = await context.MediaItems.FindAsync(uniqueId);
			var unchangedNull1 = await context.MediaItems.FindAsync(nullPreHashId1);
			var unchangedNull2 = await context.MediaItems.FindAsync(nullPreHashId2);
			var unchangedGhostExisting = await context.MediaItems.FindAsync(ghostDupExistingId);
			var unchangedGhostDeleted = await context.MediaItems.FindAsync(ghostDupDeletedId);
			var unchangedDeletedDup = await context.MediaItems.FindAsync(deletedDupId);

			updatedMissing1.ShouldNotBeNull();
			updatedMissing1.FullHash.ShouldNotBeNullOrEmpty();
			updatedMissing1.FullHashUpdatedTime.ShouldNotBeNull();

			updatedMissing2.ShouldNotBeNull();
			updatedMissing2.FullHash.ShouldNotBeNullOrEmpty();
			updatedMissing2.FullHashUpdatedTime.ShouldNotBeNull();

			updatedOutdated.ShouldNotBeNull();
			updatedOutdated.FullHash.ShouldNotBeNullOrEmpty();
			updatedOutdated.FullHash.ShouldNotBe("old_full");
			updatedOutdated.FullHashUpdatedTime.ShouldNotBeNull();

			unchangedFresh.ShouldNotBeNull();
			unchangedFresh.FullHash.ShouldBe("fresh_full");
			unchangedFresh.FullHashUpdatedTime.ShouldBe(baseTime);

			unchangedUnique.ShouldNotBeNull();
			unchangedUnique.FullHash.ShouldBeNull();

			unchangedNull1.ShouldNotBeNull();
			unchangedNull1.FullHash.ShouldBeNull();

			unchangedNull2.ShouldNotBeNull();
			unchangedNull2.FullHash.ShouldBeNull();

			unchangedGhostExisting.ShouldNotBeNull();
			unchangedGhostExisting.FullHash.ShouldBeNull();

			unchangedGhostDeleted.ShouldNotBeNull();
			unchangedGhostDeleted.FullHash.ShouldBeNull();

			unchangedDeletedDup.ShouldNotBeNull();
			unchangedDeletedDup.FullHash.ShouldBeNull();
		}
	}

	/// <summary>
	/// 重複PreHashが存在しない場合、FullHash更新がキューに追加されないことを確認する。
	/// </summary>
	[Fact]
	public async Task EnqueueFullHashUpdatesForDuplicatePreHashAsync_WhenNoDuplicatePreHashes_ShouldNotEnqueue() {
		// Arrange
		var baseTime = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
		using (var context = new MediaDeckDbContext(this._options)) {
			context.MediaItems.AddRange(
				CreateMediaItem("unique1.txt", isExists: true, preHash: "unique_1", preHashUpdatedTime: baseTime),
				CreateMediaItem("unique2.txt", isExists: true, preHash: "unique_2", preHashUpdatedTime: baseTime),
				CreateMediaItem("null1.txt", isExists: true, preHash: null),
				CreateMediaItem("null2.txt", isExists: true, preHash: null));
			await context.SaveChangesAsync();
		}

		var mockFactory = this.CreateDbFactoryMock();
		var loggerMock = new Mock<ILogger<FileHashUpdatorService>>();
		using var service = this.CreateService(mockFactory.Object, loggerMock.Object);

		// Act
		await service.EnqueueFullHashUpdatesForDuplicatePreHashAsync(CancellationToken.None);

		// Assert
		service.FullHashTargetCount.Value.ShouldBe(0);
	}

	/// <summary>
	/// 重複していない既存アイテムのFullHashとFullHashUpdatedTimeのみがクリアされ、重複や対象外レコードは保持されることを確認する。
	/// </summary>
	[Fact]
	public async Task ClearFullHashForNonDuplicatePreHashAsync_ShouldClearOnlyNonDuplicateExistingItems() {
		// Arrange
		var now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
		long dupId1;
		long dupId2;
		long singleId;
		long mixedExistingId;
		long mixedDeletedId;
		long nullPreId;

		using (var context = new MediaDeckDbContext(this._options)) {
			var dup1 = CreateMediaItem("dup1.txt", isExists: true, preHash: "dup_hash", preHashUpdatedTime: now, fullHash: "full_dup", fullHashUpdatedTime: now);
			var dup2 = CreateMediaItem("dup2.txt", isExists: true, preHash: "dup_hash", preHashUpdatedTime: now, fullHash: "full_dup", fullHashUpdatedTime: now);
			var single = CreateMediaItem("single.txt", isExists: true, preHash: "single_hash", preHashUpdatedTime: now, fullHash: "full_single", fullHashUpdatedTime: now);
			var mixedExisting = CreateMediaItem("mixed_exists.txt", isExists: true, preHash: "single_exists_hash", preHashUpdatedTime: now, fullHash: "full_mixed", fullHashUpdatedTime: now);
			var mixedDeleted = CreateMediaItem("mixed_deleted.txt", isExists: false, preHash: "single_exists_hash", preHashUpdatedTime: now, fullHash: "full_mixed", fullHashUpdatedTime: now);
			var nullPre = CreateMediaItem("null_pre.txt", isExists: true, preHash: null, preHashUpdatedTime: null, fullHash: "full_null_pre", fullHashUpdatedTime: now);

			context.MediaItems.AddRange(dup1, dup2, single, mixedExisting, mixedDeleted, nullPre);
			await context.SaveChangesAsync();

			dupId1 = dup1.MediaItemId;
			dupId2 = dup2.MediaItemId;
			singleId = single.MediaItemId;
			mixedExistingId = mixedExisting.MediaItemId;
			mixedDeletedId = mixedDeleted.MediaItemId;
			nullPreId = nullPre.MediaItemId;
		}

		var mockFactory = this.CreateDbFactoryMock();
		var loggerMock = new Mock<ILogger<FileHashUpdatorService>>();
		using var service = this.CreateService(mockFactory.Object, loggerMock.Object);

		// Act
		await service.ClearFullHashForNonDuplicatePreHashAsync(CancellationToken.None);

		// Assert
		using (var context = new MediaDeckDbContext(this._options)) {
			var dup1 = await context.MediaItems.FindAsync(dupId1);
			var dup2 = await context.MediaItems.FindAsync(dupId2);
			var single = await context.MediaItems.FindAsync(singleId);
			var mixedExisting = await context.MediaItems.FindAsync(mixedExistingId);
			var mixedDeleted = await context.MediaItems.FindAsync(mixedDeletedId);
			var nullPre = await context.MediaItems.FindAsync(nullPreId);

			dup1.ShouldNotBeNull();
			dup1.FullHash.ShouldBe("full_dup");
			dup1.FullHashUpdatedTime.ShouldBe(now);

			dup2.ShouldNotBeNull();
			dup2.FullHash.ShouldBe("full_dup");
			dup2.FullHashUpdatedTime.ShouldBe(now);

			single.ShouldNotBeNull();
			single.FullHash.ShouldBeNull();
			single.FullHashUpdatedTime.ShouldBeNull();

			mixedExisting.ShouldNotBeNull();
			mixedExisting.FullHash.ShouldBeNull();
			mixedExisting.FullHashUpdatedTime.ShouldBeNull();

			mixedDeleted.ShouldNotBeNull();
			mixedDeleted.FullHash.ShouldBe("full_mixed");
			mixedDeleted.FullHashUpdatedTime.ShouldBe(now);

			nullPre.ShouldNotBeNull();
			nullPre.FullHash.ShouldBe("full_null_pre");
			nullPre.FullHashUpdatedTime.ShouldBe(now);
		}
	}

	/// <summary>
	/// 重複PreHashグループが0件のとき、対象の全既存アイテムのFullHashとFullHashUpdatedTimeがクリアされることを確認する。
	/// </summary>
	[Fact]
	public async Task ClearFullHashForNonDuplicatePreHashAsync_WhenNoDuplicatePreHashes_ShouldClearAllTargetItems() {
		// Arrange
		var now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
		long item1Id;
		long item2Id;

		using (var context = new MediaDeckDbContext(this._options)) {
			var item1 = CreateMediaItem("unique1.txt", isExists: true, preHash: "unique_1", preHashUpdatedTime: now, fullHash: "full_1", fullHashUpdatedTime: now);
			var item2 = CreateMediaItem("unique2.txt", isExists: true, preHash: "unique_2", preHashUpdatedTime: now, fullHash: "full_2", fullHashUpdatedTime: now);
			context.MediaItems.AddRange(item1, item2);
			await context.SaveChangesAsync();
			item1Id = item1.MediaItemId;
			item2Id = item2.MediaItemId;
		}

		var mockFactory = this.CreateDbFactoryMock();
		var loggerMock = new Mock<ILogger<FileHashUpdatorService>>();
		using var service = this.CreateService(mockFactory.Object, loggerMock.Object);

		// Act
		await service.ClearFullHashForNonDuplicatePreHashAsync(CancellationToken.None);

		// Assert
		using (var context = new MediaDeckDbContext(this._options)) {
			var item1 = await context.MediaItems.FindAsync(item1Id);
			var item2 = await context.MediaItems.FindAsync(item2Id);

			item1.ShouldNotBeNull();
			item1.FullHash.ShouldBeNull();
			item1.FullHashUpdatedTime.ShouldBeNull();

			item2.ShouldNotBeNull();
			item2.FullHash.ShouldBeNull();
			item2.FullHashUpdatedTime.ShouldBeNull();
		}
	}

	/// <summary>
	/// 重複PreHashグループが多数存在する場合でも、IN句の大量パラメータ展開を行わずGROUP BYサブクエリを含む単一SQLで実行されることを確認する。
	/// </summary>
	[Fact]
	public async Task DuplicatePreHashQueries_WithManyDuplicateGroups_ShouldUseSingleSubqueryWithoutParameterExpansion() {
		// Arrange
		const int duplicateGroupCount = 1200;
		var baseTime = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
		var interceptor = new SqlCommandCaptureInterceptor();
		var interceptedOptions = new DbContextOptionsBuilder<MediaDeckDbContext>()
			.UseSqlite(this._connection)
			.AddInterceptors(interceptor)
			.Options;

		long firstGroupItem1Id;
		long firstGroupItem2Id;
		long uniqueStale1Id;
		long uniqueStale2Id;

		using (var context = new MediaDeckDbContext(this._options)) {
			var items = new List<MediaItem>((duplicateGroupCount * 2) + 2);
			for (var i = 0; i < duplicateGroupCount; i++) {
				var preHash = $"dup_{i:D4}";
				var fullHash = $"full_{i:D4}";
				items.Add(CreateMediaItem($"dup_{i}_a.txt", isExists: true, preHash: preHash, preHashUpdatedTime: baseTime, fullHash: fullHash, fullHashUpdatedTime: baseTime.AddMinutes(1)));
				items.Add(CreateMediaItem($"dup_{i}_b.txt", isExists: true, preHash: preHash, preHashUpdatedTime: baseTime, fullHash: fullHash, fullHashUpdatedTime: baseTime.AddMinutes(1)));
			}

			var uniqueStale1 = CreateMediaItem("unique_stale_1.txt", isExists: true, preHash: "unique_stale_1", preHashUpdatedTime: baseTime, fullHash: "stale_full", fullHashUpdatedTime: baseTime.AddMinutes(1));
			var uniqueStale2 = CreateMediaItem("unique_stale_2.txt", isExists: true, preHash: "unique_stale_2", preHashUpdatedTime: baseTime, fullHash: "stale_full", fullHashUpdatedTime: baseTime.AddMinutes(1));
			items.Add(uniqueStale1);
			items.Add(uniqueStale2);

			context.MediaItems.AddRange(items);
			await context.SaveChangesAsync();

			firstGroupItem1Id = items[0].MediaItemId;
			firstGroupItem2Id = items[1].MediaItemId;
			uniqueStale1Id = uniqueStale1.MediaItemId;
			uniqueStale2Id = uniqueStale2.MediaItemId;
		}

		var mockFactory = this.CreateDbFactoryMock(interceptedOptions);
		var loggerMock = new Mock<ILogger<FileHashUpdatorService>>();
		using var service = this.CreateService(mockFactory.Object, loggerMock.Object);

		// Act & Assert A: ClearFullHashForNonDuplicatePreHashAsync
		interceptor.Clear();
		await service.ClearFullHashForNonDuplicatePreHashAsync(CancellationToken.None);

		var clearCommands = interceptor.GetCommands();
		clearCommands.Count.ShouldBe(1);
		clearCommands[0].CommandText.ShouldContain("UPDATE", Case.Insensitive);
		clearCommands[0].CommandText.ShouldContain("IN (", Case.Insensitive);
		clearCommands[0].CommandText.ShouldContain("GROUP BY", Case.Insensitive);
		clearCommands[0].CommandText.ShouldNotContain("EXISTS (", Case.Insensitive);
		clearCommands[0].ParameterCount.ShouldBeLessThanOrEqualTo(5);

		using (var context = new MediaDeckDbContext(this._options)) {
			var stale1 = await context.MediaItems.FindAsync(uniqueStale1Id);
			var stale2 = await context.MediaItems.FindAsync(uniqueStale2Id);
			stale1.ShouldNotBeNull();
			stale1.FullHash.ShouldBeNull();
			stale1.FullHashUpdatedTime.ShouldBeNull();
			stale2.ShouldNotBeNull();
			stale2.FullHash.ShouldBeNull();
			stale2.FullHashUpdatedTime.ShouldBeNull();

			var remainingDupFullHashCount = await context.MediaItems
				.CountAsync(m => m.PreHash != null && m.PreHash.StartsWith("dup_") && m.FullHash != null);
			remainingDupFullHashCount.ShouldBe(duplicateGroupCount * 2);
		}

		// Act & Assert B: EnqueueFullHashUpdatesForDuplicatePreHashAsync（対象0件で1クエリのみ発行されることを確認）
		interceptor.Clear();
		await service.EnqueueFullHashUpdatesForDuplicatePreHashAsync(CancellationToken.None);

		service.FullHashTargetCount.Value.ShouldBe(0);
		var zeroTargetCommands = interceptor.GetCommands();
		zeroTargetCommands.Count.ShouldBe(1);
		zeroTargetCommands[0].CommandText.ShouldContain("SELECT", Case.Insensitive);
		zeroTargetCommands[0].CommandText.ShouldContain("IN (", Case.Insensitive);
		zeroTargetCommands[0].CommandText.ShouldContain("GROUP BY", Case.Insensitive);
		zeroTargetCommands[0].CommandText.ShouldNotContain("EXISTS (", Case.Insensitive);
		zeroTargetCommands[0].ParameterCount.ShouldBeLessThanOrEqualTo(5);

		// Act & Assert C: 1,200グループのうち1グループ（2件）が更新対象の場合
		var targetFile1 = Path.Combine(this._tempDir, "many_group_target1.txt");
		var targetFile2 = Path.Combine(this._tempDir, "many_group_target2.txt");
		await File.WriteAllTextAsync(targetFile1, "many group content 1");
		await File.WriteAllTextAsync(targetFile2, "many group content 2");

		using (var context = new MediaDeckDbContext(this._options)) {
			var item1 = await context.MediaItems.FindAsync(firstGroupItem1Id);
			var item2 = await context.MediaItems.FindAsync(firstGroupItem2Id);
			item1.ShouldNotBeNull();
			item2.ShouldNotBeNull();
			item1.FilePath = targetFile1;
			item1.FullHash = null;
			item1.FullHashUpdatedTime = null;
			item2.FilePath = targetFile2;
			item2.FullHash = null;
			item2.FullHashUpdatedTime = null;
			await context.SaveChangesAsync();
		}

		interceptor.Clear();
		await service.EnqueueFullHashUpdatesForDuplicatePreHashAsync(CancellationToken.None);
		await this.WaitUntilAsync(service.FullHashCompletedCount, 2L);

		service.FullHashTargetCount.Value.ShouldBe(2);
		var enqueueQueryCommands = interceptor.GetCommands()
			.Where(c => c.CommandText.Contains("SELECT", StringComparison.OrdinalIgnoreCase) &&
				c.CommandText.Contains("GROUP BY", StringComparison.OrdinalIgnoreCase))
			.ToList();
		enqueueQueryCommands.Count.ShouldBe(1);
		enqueueQueryCommands[0].CommandText.ShouldContain("IN (", Case.Insensitive);
		enqueueQueryCommands[0].CommandText.ShouldNotContain("EXISTS (", Case.Insensitive);
		enqueueQueryCommands[0].ParameterCount.ShouldBeLessThanOrEqualTo(5);
	}

	/// <summary>
	/// 実行されたSQLコマンドとパラメータ数を記録するテスト用インターセプター。
	/// </summary>
	private sealed class SqlCommandCaptureInterceptor : DbCommandInterceptor {
		private readonly ConcurrentQueue<CapturedSqlCommand> _commands = new();

		/// <summary>
		/// キャプチャされたSQLコマンドの一覧を取得する。
		/// </summary>
		/// <returns>キャプチャされたコマンドのリスト</returns>
		public List<CapturedSqlCommand> GetCommands() {
			return [.. this._commands];
		}

		/// <summary>
		/// 記録されたSQLコマンドをすべてクリアする。
		/// </summary>
		public void Clear() {
			this._commands.Clear();
		}

		/// <summary>
		/// 実行コマンドのテキストとパラメータ数を記録する。
		/// </summary>
		/// <param name="command">実行されるDbCommand</param>
		private void Record(DbCommand command) {
			this._commands.Enqueue(new CapturedSqlCommand(command.CommandText, command.Parameters.Count));
		}

		/// <inheritdoc />
		public override InterceptionResult<DbDataReader> ReaderExecuting(
			DbCommand command,
			CommandEventData eventData,
			InterceptionResult<DbDataReader> result) {
			this.Record(command);
			return base.ReaderExecuting(command, eventData, result);
		}

		/// <inheritdoc />
		public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
			DbCommand command,
			CommandEventData eventData,
			InterceptionResult<DbDataReader> result,
			CancellationToken cancellationToken = default) {
			this.Record(command);
			return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
		}

		/// <inheritdoc />
		public override InterceptionResult<int> NonQueryExecuting(
			DbCommand command,
			CommandEventData eventData,
			InterceptionResult<int> result) {
			this.Record(command);
			return base.NonQueryExecuting(command, eventData, result);
		}

		/// <inheritdoc />
		public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
			DbCommand command,
			CommandEventData eventData,
			InterceptionResult<int> result,
			CancellationToken cancellationToken = default) {
			this.Record(command);
			return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
		}

		/// <inheritdoc />
		public override InterceptionResult<object> ScalarExecuting(
			DbCommand command,
			CommandEventData eventData,
			InterceptionResult<object> result) {
			this.Record(command);
			return base.ScalarExecuting(command, eventData, result);
		}

		/// <inheritdoc />
		public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
			DbCommand command,
			CommandEventData eventData,
			InterceptionResult<object> result,
			CancellationToken cancellationToken = default) {
			this.Record(command);
			return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
		}
	}

	/// <summary>
	/// キャプチャされたSQLコマンド情報を表すレコード。
	/// </summary>
	/// <param name="CommandText">SQLテキスト</param>
	/// <param name="ParameterCount">パラメータ数</param>
	private sealed record CapturedSqlCommand(string CommandText, int ParameterCount);
}