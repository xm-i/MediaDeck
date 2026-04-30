using System.Diagnostics;
using MediaDeck.Composition.Database;
using MediaDeck.Composition.Tables;
using MediaDeck.Core.Services.FileChangeMonitor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit.Abstractions;

namespace MediaDeck.Core.Tests.Models.Services;

public class FileChangeTrackerTests {
	private readonly ITestOutputHelper _output;

	public FileChangeTrackerTests(ITestOutputHelper output) {
		this._output = output;
	}

	private class TestDbContextFactory : IDbContextFactory<MediaDeckDbContext> {
		private readonly DbContextOptions<MediaDeckDbContext> _options;

		public TestDbContextFactory(DbContextOptions<MediaDeckDbContext> options) {
			this._options = options;
		}

		public MediaDeckDbContext CreateDbContext() {
			var db = new MediaDeckDbContext(this._options);
			db.Database.EnsureCreated();
			return db;
		}
	}

	[Fact]
	public async Task ProcessPendingChangesAsync_PerformanceTest() {
		// 準備：インメモリデータベースのセットアップ
		var options = new DbContextOptionsBuilder<MediaDeckDbContext>()
			.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
			.Options;

		var dbFactory = new TestDbContextFactory(options);

		// データベースにダミーデータを登録
		const int testSize = 500;
		using (var db = dbFactory.CreateDbContext()) {
			for (int i = 0; i < testSize; i++) {
				db.MediaItems.Add(new MediaItem {
					ItemType = ItemType.Image,
					MediaItemId = i + 1,
					FilePath = $"/test/path/file{i}.txt",
					DirectoryPath = "/test/path",
					Description = $"Test file {i}",
					PreHash = $"hash{i}",
					FileSize = i * 1000,
					IsUnderFolderGroup = false
				});
			}
			await db.SaveChangesAsync();
		}

		// FileChangeTracker のインスタンスを作成
		var logger = NullLogger<FileChangeTracker>.Instance;
		using var tracker = new FileChangeTracker(dbFactory, logger);

		// Tracker に未処理の変更（Deleted）を複数追加
		for (int i = 0; i < testSize; i++) {
			tracker.OnDeleted($"/test/path/file{i}.txt");
		}

		// 計測
		var stopwatch = Stopwatch.StartNew();

		// Reflection を使って ProcessPendingChangesAsync を呼び出す（privateメソッドのため）
		var method = typeof(FileChangeTracker).GetMethod("ProcessPendingChangesAsync",
			System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

		var task = (Task?)method?.Invoke(tracker, null);
		if (task != null) {
			await task;
		}

		stopwatch.Stop();

		this._output.WriteLine($"Processed {testSize} pending items in {stopwatch.ElapsedMilliseconds} ms.");

		// 検証：未処理リストから Pending フラグが消えていることを確認
		// ConsolidateEvents の処理で Deleted のみで MediaItemId が存在する場合（DBから引けた場合）、
		// ConsolidateEvents でも残るが、もし追加以外で MediaItemId が null だと削除される。
		// ここでは DB から取得できたため、IsPending は false になり、リストに残る。
		Assert.Equal(testSize, tracker.UnprocessedChanges.Count);
		foreach (var change in tracker.UnprocessedChanges) {
			Assert.False(change.IsPending);
			Assert.NotNull(change.MediaItemId); // DBから取得できたはず
		}
	}
}