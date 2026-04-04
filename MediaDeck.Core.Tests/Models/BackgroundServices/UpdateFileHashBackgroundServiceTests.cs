using System.Diagnostics;

using MediaDeck.Core.Models.BackgroundServices;
using MediaDeck.Database;
using MediaDeck.Database.Tables;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Moq;

namespace MediaDeck.Core.Tests.Models.BackgroundServices;

public class UpdateFileHashBackgroundServiceTests {
	private readonly DbContextOptions<MediaDeckDbContext> _options;

	public UpdateFileHashBackgroundServiceTests() {
		this._options = new DbContextOptionsBuilder<MediaDeckDbContext>()
			.UseSqlite("DataSource=:memory:")
			.Options;
	}

	[Fact]
	public async Task ClearFullHashForNonDuplicatePreHashAsync_PerformanceTest() {
		// Arrange
		using var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
		connection.Open();
		var options = new DbContextOptionsBuilder<MediaDeckDbContext>()
			.UseSqlite(connection)
			.Options;

		using (var context = new MediaDeckDbContext(options)) {
			context.Database.EnsureCreated();

			// 大量のデータを作成
			// 重複しない PreHash を持ち、FullHash が設定されているレコードを 1000 件作成
			var mediaFiles = new List<MediaFile>();
			for (int i = 0; i < 1000; i++) {
				mediaFiles.Add(new MediaFile {
					FilePath = $"path{i}.jpg",
					DirectoryPath = "dir",
					Description = "",
					IsExists = true,
					PreHash = $"pre{i}",
					FullHash = $"full{i}",
					FullHashUpdatedTime = DateTime.Now
				});
			}
			context.MediaFiles.AddRange(mediaFiles);
			await context.SaveChangesAsync();
		}

		var dbFactoryMock = new Mock<IDbContextFactory<MediaDeckDbContext>>();
		dbFactoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
			.Returns(() => Task.FromResult(new MediaDeckDbContext(options)));

		var loggerMock = new Mock<ILogger<UpdateFileHashBackgroundService>>();
		var service = new UpdateFileHashBackgroundService(dbFactoryMock.Object, loggerMock.Object);

		// private メソッドを呼び出すためにリフレクションを使用
		var method = typeof(UpdateFileHashBackgroundService).GetMethod("ClearFullHashForNonDuplicatePreHashAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		Assert.NotNull(method);

		// Act
		var sw = Stopwatch.StartNew();
		var task = (Task)method.Invoke(service, null)!;
		await task;
		sw.Stop();

		// Assert
		using (var context = new MediaDeckDbContext(options)) {
			var remainingFullHashes = await context.MediaFiles.CountAsync(m => m.FullHash != null);
			Assert.Equal(0, remainingFullHashes);
		}

		Console.WriteLine($"Execution time for 1000 records: {sw.ElapsedMilliseconds}ms");
	}
}