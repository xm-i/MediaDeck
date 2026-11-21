using MediaDeck.Composition.Database;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Tables;
using MediaDeck.Core.Services.DuplicateFileDetector;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace MediaDeck.Core.Tests.Services.DuplicateFileDetector;

/// <summary>
/// <see cref="DuplicateFileDetectorService"/> のユニットテスト。
/// </summary>
public sealed class DuplicateFileDetectorServiceTests : IDisposable {
	private readonly List<SqliteTestDbFactory> _dbFactories = [];

	/// <summary>
	/// テストで使用したデータベースファクトリを解放します。
	/// </summary>
	public void Dispose() {
		foreach (var factory in this._dbFactories) {
			factory.Dispose();
		}
	}

	/// <summary>
	/// テスト用の SQLite インメモリデータベースファクトリを生成します。
	/// </summary>
	/// <returns>データベースコンテキストファクトリ</returns>
	private IDbContextFactory<MediaDeckDbContext> CreateSqliteDbFactory() {
		var factory = new SqliteTestDbFactory();
		this._dbFactories.Add(factory);
		return factory;
	}

	/// <summary>
	/// FullHashを使用して重複ファイルが正しく検出されることを検証します。
	/// </summary>
	[Fact]
	public async Task DetectDuplicatesAsync_WithFullHash_ShouldFindDuplicates() {
		var dbFactory = this.CreateSqliteDbFactory();

		await using (var db = await dbFactory.CreateDbContextAsync()) {
			db.MediaItems.Add(new MediaItem { MediaItemId = 1, MediaType = MediaType.Image, FilePath = "a.jpg", FullHash = "hash1", DirectoryPath = "dir", Description = "", IsUnderFolderGroup = false });
			db.MediaItems.Add(new MediaItem { MediaItemId = 2, MediaType = MediaType.Image, FilePath = "b.jpg", FullHash = "hash1", DirectoryPath = "dir", Description = "", IsUnderFolderGroup = false });
			db.MediaItems.Add(new MediaItem { MediaItemId = 3, MediaType = MediaType.Image, FilePath = "c.jpg", FullHash = "hash2", DirectoryPath = "dir", Description = "", IsUnderFolderGroup = false });
			await db.SaveChangesAsync();
		}

		var service = new DuplicateFileDetectorService(dbFactory);
		await service.DetectDuplicatesAsync(useFullHash: true);

		service.DuplicateGroups.Count.ShouldBe(1);
		service.DuplicateGroups[0].Hash.ShouldBe("hash1");
		service.DuplicateGroups[0].Files.Count.ShouldBe(2);
		service.DuplicateGroups[0].Files.ShouldContain(x => x.MediaItemId == 1);
		service.DuplicateGroups[0].Files.ShouldContain(x => x.MediaItemId == 2);

		service.DuplicateGroupCount.Value.ShouldBe(1);
		service.DuplicateFileCount.Value.ShouldBe(2);
		service.IsCompleted.Value.ShouldBeTrue();
		service.IsDetecting.Value.ShouldBeFalse();
	}

	/// <summary>
	/// PreHashを使用して重複ファイルが正しく検出されることを検証します。
	/// </summary>
	[Fact]
	public async Task DetectDuplicatesAsync_WithPreHash_ShouldFindDuplicates() {
		var dbFactory = this.CreateSqliteDbFactory();

		await using (var db = await dbFactory.CreateDbContextAsync()) {
			db.MediaItems.Add(new MediaItem { MediaItemId = 1, MediaType = MediaType.Image, FilePath = "a.jpg", PreHash = "pre1", DirectoryPath = "dir", Description = "", IsUnderFolderGroup = false });
			db.MediaItems.Add(new MediaItem { MediaItemId = 2, MediaType = MediaType.Image, FilePath = "b.jpg", PreHash = "pre1", DirectoryPath = "dir", Description = "", IsUnderFolderGroup = false });
			db.MediaItems.Add(new MediaItem { MediaItemId = 3, MediaType = MediaType.Image, FilePath = "c.jpg", PreHash = "pre2", DirectoryPath = "dir", Description = "", IsUnderFolderGroup = false });
			await db.SaveChangesAsync();
		}

		var service = new DuplicateFileDetectorService(dbFactory);
		await service.DetectDuplicatesAsync(useFullHash: false);

		service.DuplicateGroups.Count.ShouldBe(1);
		service.DuplicateGroups[0].Hash.ShouldBe("pre1");
		service.DuplicateGroupCount.Value.ShouldBe(1);
		service.DuplicateFileCount.Value.ShouldBe(2);
	}

	/// <summary>
	/// 重複グループとグループ内のファイルが正しくソートされることを検証します。
	/// </summary>
	[Fact]
	public async Task DetectDuplicatesAsync_ShouldSortGroupsAndFiles() {
		var dbFactory = this.CreateSqliteDbFactory();

		await using (var db = await dbFactory.CreateDbContextAsync()) {
			// グループB
			db.MediaItems.Add(new MediaItem { MediaItemId = 1, MediaType = MediaType.Image, FilePath = "z.jpg", FullHash = "hashB", DirectoryPath = "dir", Description = "", IsUnderFolderGroup = false });
			db.MediaItems.Add(new MediaItem { MediaItemId = 2, MediaType = MediaType.Image, FilePath = "y.jpg", FullHash = "hashB", DirectoryPath = "dir", Description = "", IsUnderFolderGroup = false });
			// グループA
			db.MediaItems.Add(new MediaItem { MediaItemId = 3, MediaType = MediaType.Image, FilePath = "b.jpg", FullHash = "hashA", DirectoryPath = "dir", Description = "", IsUnderFolderGroup = false });
			db.MediaItems.Add(new MediaItem { MediaItemId = 4, MediaType = MediaType.Image, FilePath = "a.jpg", FullHash = "hashA", DirectoryPath = "dir", Description = "", IsUnderFolderGroup = false });
			await db.SaveChangesAsync();
		}

		var service = new DuplicateFileDetectorService(dbFactory);
		await service.DetectDuplicatesAsync(useFullHash: true);

		service.DuplicateGroups.Count.ShouldBe(2);

		// グループは代表ファイル名（グループ内の最初のファイルパス）でソートされる
		// グループAの代表: "a.jpg" (ファイルソート後)
		// グループBの代表: "y.jpg" (ファイルソート後)
		// したがって、グループAが先に来るはず

		service.DuplicateGroups[0].Hash.ShouldBe("hashA");
		service.DuplicateGroups[0].Files[0].FilePath.ShouldBe("a.jpg");
		service.DuplicateGroups[0].Files[1].FilePath.ShouldBe("b.jpg");

		service.DuplicateGroups[1].Hash.ShouldBe("hashB");
		service.DuplicateGroups[1].Files[0].FilePath.ShouldBe("y.jpg");
		service.DuplicateGroups[1].Files[1].FilePath.ShouldBe("z.jpg");
	}

	/// <summary>
	/// 再実行時に前回の結果がクリアされることを検証します。
	/// </summary>
	[Fact]
	public async Task DetectDuplicatesAsync_ShouldClearPreviousResults() {
		var dbFactory = this.CreateSqliteDbFactory();

		await using (var db = await dbFactory.CreateDbContextAsync()) {
			db.MediaItems.Add(new MediaItem { MediaItemId = 1, MediaType = MediaType.Image, FilePath = "a.jpg", FullHash = "hash1", DirectoryPath = "dir", Description = "", IsUnderFolderGroup = false });
			db.MediaItems.Add(new MediaItem { MediaItemId = 2, MediaType = MediaType.Image, FilePath = "b.jpg", FullHash = "hash1", DirectoryPath = "dir", Description = "", IsUnderFolderGroup = false });
			await db.SaveChangesAsync();
		}

		var service = new DuplicateFileDetectorService(dbFactory);
		await service.DetectDuplicatesAsync(useFullHash: true);
		service.DuplicateGroups.Count.ShouldBe(1);

		// データベースを更新して重複を解消
		await using (var db = await dbFactory.CreateDbContextAsync()) {
			var item2 = await db.MediaItems.FindAsync(2L);
			item2!.FullHash = "hash2";
			await db.SaveChangesAsync();
		}

		await service.DetectDuplicatesAsync(useFullHash: true);
		service.DuplicateGroups.Count.ShouldBe(0);
		service.DuplicateGroupCount.Value.ShouldBe(0);
		service.DuplicateFileCount.Value.ShouldBe(0);
	}
}