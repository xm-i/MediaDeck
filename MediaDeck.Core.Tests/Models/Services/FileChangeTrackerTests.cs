using System.Data.Common;
using System.Diagnostics;

using MediaDeck.Common.Utilities;
using MediaDeck.Composition.Database;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Composition.Stores.Config.Model.Objects;
using MediaDeck.Composition.Stores.State.Model.Objects;
using MediaDeck.Composition.Tables;
using MediaDeck.Core.Services.FileChangeMonitor;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using Shouldly;

using Xunit.Abstractions;

namespace MediaDeck.Core.Tests.Models.Services;

/// <summary>
/// <see cref="FileChangeTracker"/> の単体テストクラスです。
/// </summary>
public class FileChangeTrackerTests : IDisposable {
	private readonly ITestOutputHelper _output;
	private readonly string _tempRoot;
	private readonly List<SqliteConnection> _connections = [];
	private readonly List<ServiceProvider> _serviceProviders = [];

	/// <summary>
	/// <see cref="FileChangeTrackerTests"/> の新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="output">テスト出力ヘルパー</param>
	public FileChangeTrackerTests(ITestOutputHelper output) {
		this._output = output;
		this._tempRoot = Path.Combine(Path.GetTempPath(), $"MediaDeck_FCT_{Guid.NewGuid():N}");
		Directory.CreateDirectory(this._tempRoot);
	}

	/// <inheritdoc />
	public void Dispose() {
		foreach (var provider in this._serviceProviders) {
			provider.Dispose();
		}
		foreach (var conn in this._connections) {
			conn.Dispose();
		}
		if (Directory.Exists(this._tempRoot)) {
			try {
				Directory.Delete(this._tempRoot, recursive: true);
			} catch {
				// クリーンアップ失敗は無視
			}
		}
	}

	private IDbContextFactory<MediaDeckDbContext> CreateSqliteDbFactory() {
		var connection = new SqliteConnection("DataSource=:memory:");
		connection.Open();
		this._connections.Add(connection);

		var options = new DbContextOptionsBuilder<MediaDeckDbContext>()
			.UseSqlite(connection)
			.Options;

		using (var context = new MediaDeckDbContext(options)) {
			context.Database.EnsureCreated();
		}

		var factoryMock = new Mock<IDbContextFactory<MediaDeckDbContext>>();
		factoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(() => new MediaDeckDbContext(options));

		return factoryMock.Object;
	}

	private ConfigModel CreateTestConfig(params (string FolderPath, bool IsGroupingRoot)[] folders) {
		var services = new ServiceCollection();
		services.AddSingleton<PathConfigModel>();
		services.AddSingleton<ExecutionConfigModel>();
		services.AddTransient<ExtensionObjectModel>();
		services.AddSingleton<ScanConfigModel>();
		services.AddSingleton<ThumbnailConfigModel>();
		services.AddSingleton<SearchConfigModel>();
		services.AddSingleton<EnvironmentConfigModel>();
		services.AddSingleton<FolderManagerConfigModel>();
		services.AddSingleton<SearchDefinitionsConfigModel>();
		services.AddSingleton<LanguageConfigModel>();
		services.AddSingleton<IStringProvider, StubStringProvider>();
		services.AddSingleton<ConfigModel>();
		services.AddTransient<SortObject>(sp => new SortObject(sp));
		services.AddTransient<SortItemObject>();
		var provider = services.BuildServiceProvider();
		this._serviceProviders.Add(provider);
		var config = provider.GetRequiredService<ConfigModel>();

		foreach (var (folderPath, isGroupingRoot) in folders) {
			config.FolderManagerConfig.Folders.Add(new FolderModel {
				FolderPath = folderPath,
				IsGroupingRoot = isGroupingRoot
			});
		}

		return config;
	}

	private string CreateRealFile(string relativePath, string content = "default-content") {
		var fullPath = Path.Combine(this._tempRoot, relativePath);
		var dir = Path.GetDirectoryName(fullPath)!;
		Directory.CreateDirectory(dir);
		File.WriteAllText(fullPath, content);
		return fullPath;
	}

	private static MediaItem CreateMediaItemEntity(
		long id,
		string filePath,
		MediaType mediaType = MediaType.Image,
		string? preHash = null,
		long fileSize = 100,
		bool isUnderFolderGroup = false) {
		var dir = Path.GetDirectoryName(filePath) ?? string.Empty;
		return new MediaItem {
			MediaItemId = id,
			MediaType = mediaType,
			FilePath = filePath,
			DirectoryPath = dir,
			Description = string.Empty,
			PreHash = preHash,
			FileSize = fileSize,
			IsExists = true,
			IsUnderFolderGroup = isUnderFolderGroup
		};
	}

	[Fact]
	public async Task ProcessPendingChangesAsync_PerformanceTest() {
		var dbFactory = this.CreateSqliteDbFactory();
		const int testSize = 500;
		await using (var db = await dbFactory.CreateDbContextAsync()) {
			for (int i = 0; i < testSize; i++) {
				db.MediaItems.Add(CreateMediaItemEntity(i + 1, $"/test/path/file{i}.txt", preHash: $"hash{i}", fileSize: i * 1000));
			}
			await db.SaveChangesAsync();
		}

		using var tracker = new FileChangeTracker(dbFactory, NullLogger<FileChangeTracker>.Instance, this.CreateTestConfig());

		for (int i = 0; i < testSize; i++) {
			tracker.OnDeleted($"/test/path/file{i}.txt");
		}

		var stopwatch = Stopwatch.StartNew();
		await tracker.ProcessPendingChangesAsync();
		stopwatch.Stop();

		this._output.WriteLine($"Processed {testSize} pending items in {stopwatch.ElapsedMilliseconds} ms.");

		tracker.UnprocessedChanges.Count.ShouldBe(testSize);
		foreach (var change in tracker.UnprocessedChanges) {
			change.IsPending.ShouldBeFalse();
			change.MediaItemId.ShouldNotBeNull();
			change.ChangeType.ShouldBe(FileChangeType.Deleted);
		}
	}

	[Fact]
	public async Task ConcurrentAccess_DoesNotThrow() {
		var dbFactory = this.CreateSqliteDbFactory();
		await using (var db = await dbFactory.CreateDbContextAsync()) {
			for (int t = 0; t < 10; t++) {
				db.MediaItems.Add(CreateMediaItemEntity(t + 1, Path.Combine(this._tempRoot, $"db_{t}.jpg"), preHash: $"dbhash_{t}"));
			}
			await db.SaveChangesAsync();
		}

		using var tracker = new FileChangeTracker(dbFactory, NullLogger<FileChangeTracker>.Instance, this.CreateTestConfig());
		tracker.StabilityDelay = TimeSpan.FromMilliseconds(10);

		var tasks = new List<Task>();
		for (int t = 0; t < 10; t++) {
			int threadId = t;
			tasks.Add(Task.Run(() => {
				tracker.OnDeleted(Path.Combine(this._tempRoot, $"db_{threadId}.jpg"));
				for (int i = 0; i < 50; i++) {
					var path = Path.Combine(this._tempRoot, $"thread{threadId}_file{i}.jpg");
					tracker.OnCreated(path);
					tracker.OnChanged(path);
					tracker.OnRenamed(path, path + ".renamed.jpg");
					tracker.OnDeleted(path + ".renamed.jpg");
				}
			}));
		}

		await Task.WhenAll(tasks);
		await tracker.ProcessPendingChangesAsync();

		// DB登録済みの10件のみDeletedとして残り、作成→即削除された一時ファイルは相殺される
		tracker.UnprocessedChanges.Count.ShouldBe(10);
		tracker.UnprocessedChanges.ShouldAllBe(x => x.ChangeType == FileChangeType.Deleted);
	}

	[Fact]
	public async Task FolderRename_DetectsAllNestedRegisteredFiles() {
		var dbFactory = this.CreateSqliteDbFactory();
		var oldDir = Path.Combine(this._tempRoot, "OldFolder");
		var newDir = Path.Combine(this._tempRoot, "NewFolder");
		var oldFile1 = Path.Combine(oldDir, "a.jpg");
		var oldFile2 = Path.Combine(oldDir, "Sub", "b.png");
		var newFile1 = this.CreateRealFile(Path.Combine("NewFolder", "a.jpg"), "img-a");
		var newFile2 = this.CreateRealFile(Path.Combine("NewFolder", "Sub", "b.png"), "img-b");

		await using (var db = await dbFactory.CreateDbContextAsync()) {
			db.MediaItems.AddRange(
				CreateMediaItemEntity(1, oldFile1, preHash: FileHashUtility.ComputeFileHash(newFile1)),
				CreateMediaItemEntity(2, oldFile2, preHash: FileHashUtility.ComputeFileHash(newFile2))
			);
			await db.SaveChangesAsync();
		}

		using var tracker = new FileChangeTracker(dbFactory, NullLogger<FileChangeTracker>.Instance, this.CreateTestConfig((this._tempRoot, false)));
		tracker.OnRenamed(oldDir, newDir);
		await tracker.ProcessPendingChangesAsync();

		tracker.UnprocessedChanges.Count.ShouldBe(2);
		tracker.UnprocessedChanges.ShouldContain(x => x.MediaItemId == 1 && x.ChangeType == FileChangeType.Renamed && x.OldPath == oldFile1 && x.NewPath == newFile1);
		tracker.UnprocessedChanges.ShouldContain(x => x.MediaItemId == 2 && x.ChangeType == FileChangeType.Renamed && x.OldPath == oldFile2 && x.NewPath == newFile2);
	}

	[Fact]
	public async Task FolderGroupRename_DetectsFolderGroupAndChildFiles() {
		var dbFactory = this.CreateSqliteDbFactory();
		var oldDir = Path.Combine(this._tempRoot, "Album1");
		var newDir = Path.Combine(this._tempRoot, "Album2");
		var oldChild = Path.Combine(oldDir, "01.jpg");
		var newChild = this.CreateRealFile(Path.Combine("Album2", "01.jpg"), "album-photo");

		await using (var db = await dbFactory.CreateDbContextAsync()) {
			db.MediaItems.AddRange(
				CreateMediaItemEntity(1, oldDir, MediaType.FolderGroup, fileSize: 0),
				CreateMediaItemEntity(2, oldChild, MediaType.Image, preHash: FileHashUtility.ComputeFileHash(newChild), isUnderFolderGroup: true)
			);
			await db.SaveChangesAsync();
		}

		using var tracker = new FileChangeTracker(dbFactory, NullLogger<FileChangeTracker>.Instance, this.CreateTestConfig((this._tempRoot, true)));
		tracker.OnRenamed(oldDir, newDir);
		await tracker.ProcessPendingChangesAsync();

		tracker.UnprocessedChanges.Count.ShouldBe(2);
		tracker.UnprocessedChanges.ShouldContain(x => x.MediaItemId == 1 && x.ChangeType == FileChangeType.Renamed && x.OldPath == oldDir && x.NewPath == newDir);
		tracker.UnprocessedChanges.ShouldContain(x => x.MediaItemId == 2 && x.ChangeType == FileChangeType.Renamed && x.OldPath == oldChild && x.NewPath == newChild);
	}

	[Fact]
	public async Task FolderRename_WithMixedRegisteredAndUnregisteredFiles_DetectsBothRenamedAndAdded() {
		var dbFactory = this.CreateSqliteDbFactory();
		var oldDir = Path.Combine(this._tempRoot, "SrcDir");
		var newDir = Path.Combine(this._tempRoot, "DstDir");
		var oldRegistered = Path.Combine(oldDir, "known.jpg");
		var newRegistered = this.CreateRealFile(Path.Combine("DstDir", "known.jpg"), "known-content");
		var newUntracked = this.CreateRealFile(Path.Combine("DstDir", "extra.png"), "extra-content");

		await using (var db = await dbFactory.CreateDbContextAsync()) {
			db.MediaItems.Add(CreateMediaItemEntity(10, oldRegistered, preHash: FileHashUtility.ComputeFileHash(newRegistered)));
			await db.SaveChangesAsync();
		}

		using var tracker = new FileChangeTracker(dbFactory, NullLogger<FileChangeTracker>.Instance, this.CreateTestConfig((this._tempRoot, false)));
		tracker.OnRenamed(oldDir, newDir);
		await tracker.ProcessPendingChangesAsync();

		tracker.UnprocessedChanges.Count.ShouldBe(2);
		tracker.UnprocessedChanges.ShouldContain(x => x.MediaItemId == 10 && x.ChangeType == FileChangeType.Renamed && x.OldPath == oldRegistered && x.NewPath == newRegistered);
		tracker.UnprocessedChanges.ShouldContain(x => x.MediaItemId == null && x.ChangeType == FileChangeType.Added && x.OldPath == string.Empty && x.NewPath == newUntracked);
	}

	[Fact]
	public async Task FolderDeleted_DetectsAllNestedRegisteredFilesAsDeleted() {
		var dbFactory = this.CreateSqliteDbFactory();
		var deletedDir = Path.Combine(this._tempRoot, "DeletedFolder");
		var file1 = Path.Combine(deletedDir, "1.jpg");
		var file2 = Path.Combine(deletedDir, "Sub", "2.mp4");

		await using (var db = await dbFactory.CreateDbContextAsync()) {
			db.MediaItems.AddRange(
				CreateMediaItemEntity(1, deletedDir, MediaType.FolderGroup, fileSize: 0),
				CreateMediaItemEntity(2, file1, MediaType.Image, preHash: "h1"),
				CreateMediaItemEntity(3, file2, MediaType.Video, preHash: "h2")
			);
			await db.SaveChangesAsync();
		}

		using var tracker = new FileChangeTracker(dbFactory, NullLogger<FileChangeTracker>.Instance, this.CreateTestConfig((this._tempRoot, true)));
		tracker.OnDeleted(deletedDir);
		await tracker.ProcessPendingChangesAsync();

		tracker.UnprocessedChanges.Count.ShouldBe(3);
		tracker.UnprocessedChanges.ShouldAllBe(x => x.ChangeType == FileChangeType.Deleted);
		tracker.UnprocessedChanges.Select(x => x.MediaItemId).ShouldBe([1L, 2L, 3L], ignoreOrder: true);
	}

	[Fact]
	public async Task FolderCreated_UnderNonGroupingRoot_ExpandsFilesWithoutAddingDirectory() {
		var dbFactory = this.CreateSqliteDbFactory();
		var newDir = Path.Combine(this._tempRoot, "ImportedFolder");
		var file1 = this.CreateRealFile(Path.Combine("ImportedFolder", "a.jpg"), "aaa");
		var file2 = this.CreateRealFile(Path.Combine("ImportedFolder", "Sub", "b.mp4"), "bbb");
		this.CreateRealFile(Path.Combine("ImportedFolder", "readme.txt"), "ignored");

		using var tracker = new FileChangeTracker(dbFactory, NullLogger<FileChangeTracker>.Instance, this.CreateTestConfig((this._tempRoot, false)));
		tracker.OnCreated(newDir);
		await tracker.ProcessPendingChangesAsync();

		tracker.UnprocessedChanges.Count.ShouldBe(2);
		tracker.UnprocessedChanges.ShouldAllBe(x => x.ChangeType == FileChangeType.Added && x.MediaItemId == null);
		tracker.UnprocessedChanges.Select(x => x.NewPath).ShouldBe([file1, file2], ignoreOrder: true);
	}

	[Fact]
	public async Task FolderCreated_UnderGroupingRoot_AddsDirectChildDirectoryAndNestedFilesOnly() {
		var dbFactory = this.CreateSqliteDbFactory();
		var newAlbumDir = Path.Combine(this._tempRoot, "NewAlbum");
		var file1 = this.CreateRealFile(Path.Combine("NewAlbum", "01.jpg"), "aaa");
		var file2 = this.CreateRealFile(Path.Combine("NewAlbum", "Nested", "02.jpg"), "bbb");

		using var tracker = new FileChangeTracker(dbFactory, NullLogger<FileChangeTracker>.Instance, this.CreateTestConfig((this._tempRoot, true)));
		tracker.OnCreated(newAlbumDir);
		await tracker.ProcessPendingChangesAsync();

		tracker.UnprocessedChanges.Count.ShouldBe(3);
		tracker.UnprocessedChanges.ShouldAllBe(x => x.ChangeType == FileChangeType.Added && x.MediaItemId == null);
		tracker.UnprocessedChanges.Select(x => x.NewPath).ShouldBe([newAlbumDir, file1, file2], ignoreOrder: true);
	}

	[Fact]
	public async Task FolderMultiHopRename_BothSingleAndMultiBatch_ResolvesEndToEndPath() {
		var dbFactory = this.CreateSqliteDbFactory();
		var dirA = Path.Combine(this._tempRoot, "DirA");
		var dirB = Path.Combine(this._tempRoot, "DirB");
		var dirC = Path.Combine(this._tempRoot, "DirC");
		var oldFile = Path.Combine(dirA, "Sub", "img.jpg");
		var finalFile = this.CreateRealFile(Path.Combine("DirC", "Sub", "img.jpg"), "content");

		await using (var db = await dbFactory.CreateDbContextAsync()) {
			db.MediaItems.Add(CreateMediaItemEntity(1, oldFile, preHash: FileHashUtility.ComputeFileHash(finalFile)));
			await db.SaveChangesAsync();
		}

		// バッチをまたいだ多段リネーム (バッチ1: DirA -> DirB, バッチ2: DirB -> DirC)
		using var tracker = new FileChangeTracker(dbFactory, NullLogger<FileChangeTracker>.Instance, this.CreateTestConfig((this._tempRoot, false)));
		tracker.OnRenamed(dirA, dirB);
		// DirB が一時的に存在して DirC へ移動する流れを再現するため、まず同一バッチでテスト
		tracker.OnRenamed(dirB, dirC);
		await tracker.ProcessPendingChangesAsync();

		tracker.UnprocessedChanges.Count.ShouldBe(1);
		var item = tracker.UnprocessedChanges[0];
		item.ChangeType.ShouldBe(FileChangeType.Renamed);
		item.MediaItemId.ShouldBe(1L);
		item.OldPath.ShouldBe(oldFile);
		item.NewPath.ShouldBe(finalFile);
	}

	[Fact]
	public async Task FolderMultiHopRename_AcrossBatches_ResolvesEndToEndPath() {
		var dbFactory = this.CreateSqliteDbFactory();
		var dirA = Path.Combine(this._tempRoot, "BatchDirA");
		var dirB = Path.Combine(this._tempRoot, "BatchDirB");
		var dirC = Path.Combine(this._tempRoot, "BatchDirC");
		var oldFile = Path.Combine(dirA, "img.jpg");
		var midFile = this.CreateRealFile(Path.Combine("BatchDirB", "img.jpg"), "content");

		await using (var db = await dbFactory.CreateDbContextAsync()) {
			db.MediaItems.Add(CreateMediaItemEntity(1, oldFile, preHash: FileHashUtility.ComputeFileHash(midFile)));
			await db.SaveChangesAsync();
		}

		using var tracker = new FileChangeTracker(dbFactory, NullLogger<FileChangeTracker>.Instance, this.CreateTestConfig((this._tempRoot, false)));

		// バッチ1: DirA -> DirB
		tracker.OnRenamed(dirA, dirB);
		await tracker.ProcessPendingChangesAsync();
		tracker.UnprocessedChanges.Count.ShouldBe(1);
		tracker.UnprocessedChanges[0].NewPath.ShouldBe(midFile);

		// 実ディレクトリを DirB -> DirC に移動してバッチ2
		Directory.Move(dirB, dirC);
		var finalFile = Path.Combine(dirC, "img.jpg");
		tracker.OnRenamed(dirB, dirC);
		await tracker.ProcessPendingChangesAsync();

		tracker.UnprocessedChanges.Count.ShouldBe(1);
		var item = tracker.UnprocessedChanges[0];
		item.ChangeType.ShouldBe(FileChangeType.Renamed);
		item.MediaItemId.ShouldBe(1L);
		item.OldPath.ShouldBe(oldFile);
		item.NewPath.ShouldBe(finalFile);
	}

	[Fact]
	public async Task CreatedThenRenamed_SameBatch_ResultsInAddedWithNewPath() {
		var dbFactory = this.CreateSqliteDbFactory();
		var createdPath = Path.Combine(this._tempRoot, "NewFile.jpg");
		var renamedPath = this.CreateRealFile("RenamedFile.jpg", "new-image-bytes");

		using var tracker = new FileChangeTracker(dbFactory, NullLogger<FileChangeTracker>.Instance, this.CreateTestConfig((this._tempRoot, false)));
		tracker.OnCreated(createdPath);
		tracker.OnRenamed(createdPath, renamedPath);
		await tracker.ProcessPendingChangesAsync();

		tracker.UnprocessedChanges.Count.ShouldBe(1);
		var item = tracker.UnprocessedChanges[0];
		item.ChangeType.ShouldBe(FileChangeType.Added);
		item.MediaItemId.ShouldBeNull();
		item.OldPath.ShouldBe(string.Empty);
		item.NewPath.ShouldBe(renamedPath);
		item.NewHash.ShouldBe(FileHashUtility.ComputeFileHash(renamedPath));
	}

	[Fact]
	public async Task CreatedThenRenamed_AcrossBatches_UpdatesAddedPathWithoutBecomingRenamed() {
		var dbFactory = this.CreateSqliteDbFactory();
		var createdPath = this.CreateRealFile("Step1.jpg", "new-image-bytes");
		var renamedPath = Path.Combine(this._tempRoot, "Step2.jpg");

		using var tracker = new FileChangeTracker(dbFactory, NullLogger<FileChangeTracker>.Instance, this.CreateTestConfig((this._tempRoot, false)));

		// バッチ1: 新規作成が確定して Added として表示される
		tracker.OnCreated(createdPath);
		await tracker.ProcessPendingChangesAsync();
		tracker.UnprocessedChanges.Count.ShouldBe(1);
		tracker.UnprocessedChanges[0].ChangeType.ShouldBe(FileChangeType.Added);
		tracker.UnprocessedChanges[0].NewPath.ShouldBe(createdPath);

		// バッチ2: 確定済みの新規ファイルを名前変更
		File.Move(createdPath, renamedPath);
		tracker.OnRenamed(createdPath, renamedPath);
		await tracker.ProcessPendingChangesAsync();

		tracker.UnprocessedChanges.Count.ShouldBe(1);
		var item = tracker.UnprocessedChanges[0];
		item.ChangeType.ShouldBe(FileChangeType.Added);
		item.MediaItemId.ShouldBeNull();
		item.OldPath.ShouldBe(string.Empty);
		item.NewPath.ShouldBe(renamedPath);
	}

	[Fact]
	public async Task CreatedThenDeleted_SameAndAcrossBatches_CancelsOutToEmpty() {
		var dbFactory = this.CreateSqliteDbFactory();
		var tempFile1 = Path.Combine(this._tempRoot, "transient1.jpg");
		var tempFile2 = this.CreateRealFile("transient2.jpg", "temp");

		using var tracker = new FileChangeTracker(dbFactory, NullLogger<FileChangeTracker>.Instance, this.CreateTestConfig((this._tempRoot, false)));

		// 同一バッチ内で作成→即削除
		tracker.OnCreated(tempFile1);
		tracker.OnDeleted(tempFile1);
		await tracker.ProcessPendingChangesAsync();
		tracker.UnprocessedChanges.ShouldBeEmpty();

		// バッチを跨いで作成確定→削除
		tracker.OnCreated(tempFile2);
		await tracker.ProcessPendingChangesAsync();
		tracker.UnprocessedChanges.Count.ShouldBe(1);

		File.Delete(tempFile2);
		tracker.OnDeleted(tempFile2);
		await tracker.ProcessPendingChangesAsync();
		tracker.UnprocessedChanges.ShouldBeEmpty();
	}

	[Fact]
	public async Task ExtensionFiltering_IgnoresTempAndHandlesTargetTransitions() {
		var dbFactory = this.CreateSqliteDbFactory();
		var registeredJpg = Path.Combine(this._tempRoot, "db_img.jpg");
		var renamedToTmp = this.CreateRealFile("db_img.tmp", "img1");
		var registeredJpg2 = Path.Combine(this._tempRoot, "db_img2.jpg");
		var renamedToPng = this.CreateRealFile("db_img2.png", "img2");
		var crdownload = Path.Combine(this._tempRoot, "download.crdownload");
		var finalJpg = this.CreateRealFile("downloaded.jpg", "downloaded");
		var untrackedTxt = this.CreateRealFile("notes.txt", "text");

		await using (var db = await dbFactory.CreateDbContextAsync()) {
			db.MediaItems.AddRange(
				CreateMediaItemEntity(1, registeredJpg, preHash: "h1"),
				CreateMediaItemEntity(2, registeredJpg2, preHash: "h2")
			);
			await db.SaveChangesAsync();
		}

		using var tracker = new FileChangeTracker(dbFactory, NullLogger<FileChangeTracker>.Instance, this.CreateTestConfig((this._tempRoot, false)));

		// 1. 対象外拡張子の作成は無視される
		tracker.OnCreated(untrackedTxt);
		await tracker.ProcessPendingChangesAsync();
		tracker.UnprocessedChanges.ShouldBeEmpty();

		// 2. .crdownload 作成 → .jpg へリネームで Added になる
		tracker.OnCreated(crdownload);
		tracker.OnRenamed(crdownload, finalJpg);
		// 3. 登録済み .jpg → 非対象 .tmp へのリネームは Deleted になる
		tracker.OnRenamed(registeredJpg, renamedToTmp);
		// 4. 登録済み .jpg → 対象 .png へのリネームは Renamed になる
		tracker.OnRenamed(registeredJpg2, renamedToPng);
		await tracker.ProcessPendingChangesAsync();

		tracker.UnprocessedChanges.Count.ShouldBe(3);
		tracker.UnprocessedChanges.ShouldContain(x => x.ChangeType == FileChangeType.Added && x.NewPath == finalJpg);
		tracker.UnprocessedChanges.ShouldContain(x => x.ChangeType == FileChangeType.Deleted && x.MediaItemId == 1 && x.OldPath == registeredJpg);
		tracker.UnprocessedChanges.ShouldContain(x => x.ChangeType == FileChangeType.Renamed && x.MediaItemId == 2 && x.OldPath == registeredJpg2 && x.NewPath == renamedToPng);
	}

	[Fact]
	public async Task CrossDriveMove_SameBatch_DetectsAsMovedByHash() {
		var dbFactory = this.CreateSqliteDbFactory();
		var oldPath = Path.Combine(this._tempRoot, "Drive1", "movie.mp4");
		var newPath = this.CreateRealFile(Path.Combine("Drive2", "movie_moved.mp4"), "identical-video-payload");
		var hash = FileHashUtility.ComputeFileHash(newPath);

		await using (var db = await dbFactory.CreateDbContextAsync()) {
			db.MediaItems.Add(CreateMediaItemEntity(7, oldPath, MediaType.Video, preHash: hash, fileSize: new FileInfo(newPath).Length));
			await db.SaveChangesAsync();
		}

		using var tracker = new FileChangeTracker(dbFactory, NullLogger<FileChangeTracker>.Instance, this.CreateTestConfig((this._tempRoot, false)));
		tracker.OnDeleted(oldPath);
		tracker.OnCreated(newPath);
		await tracker.ProcessPendingChangesAsync();

		tracker.UnprocessedChanges.Count.ShouldBe(1);
		var item = tracker.UnprocessedChanges[0];
		item.ChangeType.ShouldBe(FileChangeType.Moved);
		item.MediaItemId.ShouldBe(7L);
		item.OldPath.ShouldBe(oldPath);
		item.NewPath.ShouldBe(newPath);
	}

	[Fact]
	public async Task CrossDriveMove_AcrossBatches_DeletedThenAddedAndAddedThenDeleted_BothUpgradeToMoved() {
		var dbFactory = this.CreateSqliteDbFactory();
		var oldPath1 = Path.Combine(this._tempRoot, "Src1", "a.jpg");
		var newPath1 = this.CreateRealFile(Path.Combine("Dst1", "a.jpg"), "payload-1");
		var hash1 = FileHashUtility.ComputeFileHash(newPath1);

		var oldPath2 = Path.Combine(this._tempRoot, "Src2", "b.jpg");
		var newPath2 = this.CreateRealFile(Path.Combine("Dst2", "b.jpg"), "payload-2");
		var hash2 = FileHashUtility.ComputeFileHash(newPath2);

		await using (var db = await dbFactory.CreateDbContextAsync()) {
			db.MediaItems.AddRange(
				CreateMediaItemEntity(1, oldPath1, preHash: hash1, fileSize: new FileInfo(newPath1).Length),
				CreateMediaItemEntity(2, oldPath2, preHash: hash2, fileSize: new FileInfo(newPath2).Length)
			);
			await db.SaveChangesAsync();
		}

		using var tracker = new FileChangeTracker(dbFactory, NullLogger<FileChangeTracker>.Instance, this.CreateTestConfig((this._tempRoot, false)));

		// パターンA: 先に Deleted が確定し、後続バッチで Added が来た場合
		tracker.OnDeleted(oldPath1);
		await tracker.ProcessPendingChangesAsync();
		tracker.UnprocessedChanges.Single(x => x.MediaItemId == 1).ChangeType.ShouldBe(FileChangeType.Deleted);

		tracker.OnCreated(newPath1);
		await tracker.ProcessPendingChangesAsync();
		tracker.UnprocessedChanges.Count.ShouldBe(1);
		tracker.UnprocessedChanges[0].ChangeType.ShouldBe(FileChangeType.Moved);
		tracker.UnprocessedChanges[0].MediaItemId.ShouldBe(1L);
		tracker.UnprocessedChanges[0].OldPath.ShouldBe(oldPath1);
		tracker.UnprocessedChanges[0].NewPath.ShouldBe(newPath1);

		// パターンB: 先に Added が確定し、後続バッチで Deleted が来た場合
		tracker.OnCreated(newPath2);
		await tracker.ProcessPendingChangesAsync();
		tracker.UnprocessedChanges.Single(x => x.NewPath == newPath2).ChangeType.ShouldBe(FileChangeType.Added);

		tracker.OnDeleted(oldPath2);
		await tracker.ProcessPendingChangesAsync();
		tracker.UnprocessedChanges.Count.ShouldBe(2);
		tracker.UnprocessedChanges.ShouldContain(x => x.MediaItemId == 2 && x.ChangeType == FileChangeType.Moved && x.OldPath == oldPath2 && x.NewPath == newPath2);
	}

	[Fact]
	public async Task CrossWatchedFolderMove_FolderDeletedAndCreated_PairsRegisteredFilesAsMoved() {
		var dbFactory = this.CreateSqliteDbFactory();
		var inboxRoot = Path.Combine(this._tempRoot, "Inbox");
		var libraryRoot = Path.Combine(this._tempRoot, "Library");
		Directory.CreateDirectory(inboxRoot);
		Directory.CreateDirectory(libraryRoot);

		var oldSub = Path.Combine(inboxRoot, "Batch1");
		var newSub = Path.Combine(libraryRoot, "Batch1");
		var oldFile = Path.Combine(oldSub, "pic.jpg");
		var newFile = this.CreateRealFile(Path.Combine("Library", "Batch1", "pic.jpg"), "moved-pic");
		var newExtra = this.CreateRealFile(Path.Combine("Library", "Batch1", "new.png"), "new-pic");

		await using (var db = await dbFactory.CreateDbContextAsync()) {
			db.MediaItems.Add(CreateMediaItemEntity(50, oldFile, preHash: FileHashUtility.ComputeFileHash(newFile), fileSize: new FileInfo(newFile).Length));
			await db.SaveChangesAsync();
		}

		using var tracker = new FileChangeTracker(
			dbFactory,
			NullLogger<FileChangeTracker>.Instance,
			this.CreateTestConfig((inboxRoot, false), (libraryRoot, false)));

		tracker.OnDeleted(oldSub);
		tracker.OnCreated(newSub);
		await tracker.ProcessPendingChangesAsync();

		tracker.UnprocessedChanges.Count.ShouldBe(2);
		tracker.UnprocessedChanges.ShouldContain(x => x.MediaItemId == 50 && x.ChangeType == FileChangeType.Moved && x.OldPath == oldFile && x.NewPath == newFile);
		tracker.UnprocessedChanges.ShouldContain(x => x.MediaItemId == null && x.ChangeType == FileChangeType.Added && x.NewPath == newExtra);
	}

	[Fact]
	public async Task AtomicSave_OverwriteAtSamePath_ResultsInNoChanges() {
		var dbFactory = this.CreateSqliteDbFactory();
		var file1 = this.CreateRealFile("edited1.jpg", "new-version-1");
		var tmp1 = Path.Combine(this._tempRoot, "edited1.jpg.tmp");
		var file2 = this.CreateRealFile("edited2.jpg", "new-version-2");
		var bak2 = Path.Combine(this._tempRoot, "edited2.jpg.bak");
		var tmp2 = Path.Combine(this._tempRoot, "edited2.jpg.tmp");

		await using (var db = await dbFactory.CreateDbContextAsync()) {
			db.MediaItems.AddRange(
				CreateMediaItemEntity(1, file1, preHash: "old-hash-1"),
				CreateMediaItemEntity(2, file2, preHash: "old-hash-2")
			);
			await db.SaveChangesAsync();
		}

		using var tracker = new FileChangeTracker(dbFactory, NullLogger<FileChangeTracker>.Instance, this.CreateTestConfig((this._tempRoot, false)));

		// パターン1: tmp作成 → 元ファイル削除 → tmpを元ファイル名へリネーム
		tracker.OnCreated(tmp1);
		tracker.OnDeleted(file1);
		tracker.OnRenamed(tmp1, file1);

		// パターン2: 元ファイルを.bakへリネーム → tmpを元ファイル名へリネーム → .bak削除
		tracker.OnCreated(tmp2);
		tracker.OnRenamed(file2, bak2);
		tracker.OnRenamed(tmp2, file2);
		tracker.OnDeleted(bak2);

		await tracker.ProcessPendingChangesAsync();

		tracker.UnprocessedChanges.ShouldBeEmpty();
	}

	[Fact]
	public async Task RoundTripRename_SameAndAcrossBatches_CancelsOutToEmpty() {
		var dbFactory = this.CreateSqliteDbFactory();
		var fileA = this.CreateRealFile("A.jpg", "same-file");
		var fileB = Path.Combine(this._tempRoot, "B.jpg");

		await using (var db = await dbFactory.CreateDbContextAsync()) {
			db.MediaItems.Add(CreateMediaItemEntity(1, fileA, preHash: FileHashUtility.ComputeFileHash(fileA)));
			await db.SaveChangesAsync();
		}

		using var tracker = new FileChangeTracker(dbFactory, NullLogger<FileChangeTracker>.Instance, this.CreateTestConfig((this._tempRoot, false)));

		// 同一バッチ内で往復リネーム
		tracker.OnRenamed(fileA, fileB);
		tracker.OnRenamed(fileB, fileA);
		await tracker.ProcessPendingChangesAsync();
		tracker.UnprocessedChanges.ShouldBeEmpty();

		// バッチを跨いで往復リネーム
		File.Move(fileA, fileB);
		tracker.OnRenamed(fileA, fileB);
		await tracker.ProcessPendingChangesAsync();
		tracker.UnprocessedChanges.Count.ShouldBe(1);
		tracker.UnprocessedChanges[0].ChangeType.ShouldBe(FileChangeType.Renamed);

		File.Move(fileB, fileA);
		tracker.OnRenamed(fileB, fileA);
		await tracker.ProcessPendingChangesAsync();
		tracker.UnprocessedChanges.ShouldBeEmpty();
	}

	[Fact]
	public async Task RenamedThenDeleted_ResultsInDeletedWithOriginalDbPath() {
		var dbFactory = this.CreateSqliteDbFactory();
		var fileA = Path.Combine(this._tempRoot, "Orig.jpg");
		var fileB = Path.Combine(this._tempRoot, "RenamedThenDeleted.jpg");

		await using (var db = await dbFactory.CreateDbContextAsync()) {
			db.MediaItems.Add(CreateMediaItemEntity(9, fileA, preHash: "orig-hash"));
			await db.SaveChangesAsync();
		}

		using var tracker = new FileChangeTracker(dbFactory, NullLogger<FileChangeTracker>.Instance, this.CreateTestConfig((this._tempRoot, false)));
		tracker.OnRenamed(fileA, fileB);
		tracker.OnDeleted(fileB);
		await tracker.ProcessPendingChangesAsync();

		tracker.UnprocessedChanges.Count.ShouldBe(1);
		var item = tracker.UnprocessedChanges[0];
		item.ChangeType.ShouldBe(FileChangeType.Deleted);
		item.MediaItemId.ShouldBe(9L);
		item.OldPath.ShouldBe(fileA);
		item.NewPath.ShouldBe(string.Empty);
	}

	[Fact]
	public async Task SwapRename_TwoRegisteredFiles_SwapsBothAccurately() {
		var dbFactory = this.CreateSqliteDbFactory();
		var fileA = this.CreateRealFile("SwapA.jpg", "content-was-B");
		var fileB = this.CreateRealFile("SwapB.jpg", "content-was-A");
		var fileTmp = Path.Combine(this._tempRoot, "SwapTmp.jpg");

		await using (var db = await dbFactory.CreateDbContextAsync()) {
			db.MediaItems.AddRange(
				CreateMediaItemEntity(1, fileA, preHash: "hash-A"),
				CreateMediaItemEntity(2, fileB, preHash: "hash-B")
			);
			await db.SaveChangesAsync();
		}

		using var tracker = new FileChangeTracker(dbFactory, NullLogger<FileChangeTracker>.Instance, this.CreateTestConfig((this._tempRoot, false)));
		tracker.OnRenamed(fileA, fileTmp);
		tracker.OnRenamed(fileB, fileA);
		tracker.OnRenamed(fileTmp, fileB);
		await tracker.ProcessPendingChangesAsync();

		tracker.UnprocessedChanges.Count.ShouldBe(2);
		tracker.UnprocessedChanges.ShouldContain(x => x.MediaItemId == 1 && x.ChangeType == FileChangeType.Renamed && x.OldPath == fileA && x.NewPath == fileB);
		tracker.UnprocessedChanges.ShouldContain(x => x.MediaItemId == 2 && x.ChangeType == FileChangeType.Renamed && x.OldPath == fileB && x.NewPath == fileA);
	}

	/// <summary>
	/// 大文字・小文字のみを変更するリネームが <see cref="FileChangeType.Renamed"/> として検知されることを検証します。
	/// </summary>
	[Fact]
	public async Task CaseOnlyRename_DetectsAsRenamed() {
		var dbFactory = this.CreateSqliteDbFactory();
		var oldPath = this.CreateRealFile("Photo.jpg", "case-content");
		var newPath = Path.Combine(this._tempRoot, "photo.jpg");

		await using (var db = await dbFactory.CreateDbContextAsync()) {
			db.MediaItems.Add(CreateMediaItemEntity(1, oldPath, preHash: FileHashUtility.ComputeFileHash(oldPath)));
			await db.SaveChangesAsync();
		}

		// 大文字・小文字を区別するファイルシステム（Linux等）でも移動先が実在するよう実際にリネームする
		File.Move(oldPath, newPath);

		using var tracker = new FileChangeTracker(dbFactory, NullLogger<FileChangeTracker>.Instance, this.CreateTestConfig((this._tempRoot, false)));
		tracker.OnRenamed(oldPath, newPath);
		await tracker.ProcessPendingChangesAsync();

		tracker.UnprocessedChanges.Count.ShouldBe(1);
		tracker.UnprocessedChanges[0].ChangeType.ShouldBe(FileChangeType.Renamed);
		tracker.UnprocessedChanges[0].OldPath.ShouldBe(oldPath);
		tracker.UnprocessedChanges[0].NewPath.ShouldBe(newPath);
	}

	[Fact]
	public async Task RemoveItems_DoesNotResurrectOnSubsequentEvents() {
		var dbFactory = this.CreateSqliteDbFactory();
		var ignoredAdded = this.CreateRealFile("ignored_added.jpg", "aaa");
		var nextAdded = this.CreateRealFile("next_added.jpg", "bbb");

		using var tracker = new FileChangeTracker(dbFactory, NullLogger<FileChangeTracker>.Instance, this.CreateTestConfig((this._tempRoot, false)));

		tracker.OnCreated(ignoredAdded);
		await tracker.ProcessPendingChangesAsync();
		tracker.UnprocessedChanges.Count.ShouldBe(1);

		tracker.RemoveItems([tracker.UnprocessedChanges[0]]);
		tracker.UnprocessedChanges.ShouldBeEmpty();

		tracker.OnCreated(nextAdded);
		await tracker.ProcessPendingChangesAsync();

		tracker.UnprocessedChanges.Count.ShouldBe(1);
		tracker.UnprocessedChanges[0].NewPath.ShouldBe(nextAdded);
	}

	[Fact]
	public async Task AutoDebounce_ReconcilesAutomaticallyAfterStabilityDelay() {
		var dbFactory = this.CreateSqliteDbFactory();
		var createdPath = Path.Combine(this._tempRoot, "auto1.jpg");
		var renamedPath = this.CreateRealFile("auto2.jpg", "auto-payload");

		using var tracker = new FileChangeTracker(dbFactory, NullLogger<FileChangeTracker>.Instance, this.CreateTestConfig((this._tempRoot, false)));
		tracker.StabilityDelay = TimeSpan.FromMilliseconds(60);

		tracker.OnCreated(createdPath);
		await Task.Delay(20);
		tracker.OnChanged(createdPath);
		tracker.OnRenamed(createdPath, renamedPath);

		// ProcessPendingChangesAsync を手動呼び出しせずにデバウンス完了を待つ
		var deadline = DateTime.UtcNow.AddSeconds(5);
		while (tracker.UnprocessedChanges.Count == 0 && DateTime.UtcNow < deadline) {
			await Task.Delay(25);
		}

		tracker.UnprocessedChanges.Count.ShouldBe(1);
		tracker.UnprocessedChanges[0].ChangeType.ShouldBe(FileChangeType.Added);
		tracker.UnprocessedChanges[0].NewPath.ShouldBe(renamedPath);
	}

	[Fact]
	public async Task LockedFile_RetriesAutomaticallyAndSucceedsAfterUnlock() {
		var dbFactory = this.CreateSqliteDbFactory();
		var lockedPath = this.CreateRealFile("locked.jpg", "locked-content");

		using var tracker = new FileChangeTracker(dbFactory, NullLogger<FileChangeTracker>.Instance, this.CreateTestConfig((this._tempRoot, false)));
		tracker.StabilityDelay = TimeSpan.FromMilliseconds(50);

		using (var lockStream = new FileStream(lockedPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) {
			tracker.OnCreated(lockedPath);
			await tracker.ProcessPendingChangesAsync();
			tracker.UnprocessedChanges.ShouldBeEmpty();
		}

		// ロック解放後、自動リトライによって Added が確定する
		var deadline = DateTime.UtcNow.AddSeconds(5);
		while (tracker.UnprocessedChanges.Count == 0 && DateTime.UtcNow < deadline) {
			await Task.Delay(25);
		}

		tracker.UnprocessedChanges.Count.ShouldBe(1);
		tracker.UnprocessedChanges[0].ChangeType.ShouldBe(FileChangeType.Added);
		tracker.UnprocessedChanges[0].NewPath.ShouldBe(lockedPath);
		tracker.UnprocessedChanges[0].NewHash.ShouldBe(FileHashUtility.ComputeFileHash(lockedPath));
	}

	[Fact]
	public async Task FolderCreated_WhenDestinationAlreadyHasRegisteredFiles_SkipsAlreadyRegisteredFiles() {
		var dbFactory = this.CreateSqliteDbFactory();
		var targetDir = Path.Combine(this._tempRoot, "ExistingDir");
		var existingFile = this.CreateRealFile(Path.Combine("ExistingDir", "already.jpg"), "existing");
		var brandNewFile = this.CreateRealFile(Path.Combine("ExistingDir", "brandnew.jpg"), "new");

		await using (var db = await dbFactory.CreateDbContextAsync()) {
			db.MediaItems.Add(CreateMediaItemEntity(1, existingFile, preHash: FileHashUtility.ComputeFileHash(existingFile)));
			await db.SaveChangesAsync();
		}

		using var tracker = new FileChangeTracker(dbFactory, NullLogger<FileChangeTracker>.Instance, this.CreateTestConfig((this._tempRoot, false)));
		tracker.OnCreated(targetDir);
		await tracker.ProcessPendingChangesAsync();

		tracker.UnprocessedChanges.Count.ShouldBe(1);
		tracker.UnprocessedChanges[0].ChangeType.ShouldBe(FileChangeType.Added);
		tracker.UnprocessedChanges[0].NewPath.ShouldBe(brandNewFile);
	}

	[Fact]
	public async Task RenameThenCreateNewAtOldPath_DetectsBothRenamedAndAdded() {
		var dbFactory = this.CreateSqliteDbFactory();
		var origPath = this.CreateRealFile("slot.jpg", "brand-new-replacement");
		var oldMovedPath = this.CreateRealFile("slot_old.jpg", "original-content");

		await using (var db = await dbFactory.CreateDbContextAsync()) {
			db.MediaItems.Add(CreateMediaItemEntity(1, origPath, preHash: FileHashUtility.ComputeFileHash(oldMovedPath)));
			await db.SaveChangesAsync();
		}

		using var tracker = new FileChangeTracker(dbFactory, NullLogger<FileChangeTracker>.Instance, this.CreateTestConfig((this._tempRoot, false)));
		tracker.OnRenamed(origPath, oldMovedPath);
		tracker.OnCreated(origPath);
		await tracker.ProcessPendingChangesAsync();

		tracker.UnprocessedChanges.Count.ShouldBe(2);
		tracker.UnprocessedChanges.ShouldContain(x => x.MediaItemId == 1 && x.ChangeType == FileChangeType.Renamed && x.OldPath == origPath && x.NewPath == oldMovedPath);
		tracker.UnprocessedChanges.ShouldContain(x => x.MediaItemId == null && x.ChangeType == FileChangeType.Added && x.NewPath == origPath);
	}

	[Fact]
	public async Task OverwriteMove_RegisteredFileOverwritesAnotherRegisteredFile_DetectsRenamedAndDeleted() {
		var dbFactory = this.CreateSqliteDbFactory();
		var srcPath = Path.Combine(this._tempRoot, "src.jpg");
		var dstPath = this.CreateRealFile("dst.jpg", "src-content");

		await using (var db = await dbFactory.CreateDbContextAsync()) {
			db.MediaItems.AddRange(
				CreateMediaItemEntity(1, srcPath, preHash: FileHashUtility.ComputeFileHash(dstPath)),
				CreateMediaItemEntity(2, dstPath, preHash: "old-dst-hash")
			);
			await db.SaveChangesAsync();
		}

		using var tracker = new FileChangeTracker(dbFactory, NullLogger<FileChangeTracker>.Instance, this.CreateTestConfig((this._tempRoot, false)));
		tracker.OnDeleted(dstPath);
		tracker.OnRenamed(srcPath, dstPath);
		await tracker.ProcessPendingChangesAsync();

		tracker.UnprocessedChanges.Count.ShouldBe(2);
		tracker.UnprocessedChanges.ShouldContain(x => x.MediaItemId == 1 && x.ChangeType == FileChangeType.Renamed && x.OldPath == srcPath && x.NewPath == dstPath);
		tracker.UnprocessedChanges.ShouldContain(x => x.MediaItemId == 2 && x.ChangeType == FileChangeType.Deleted && x.OldPath == dstPath);
	}

	[Fact]
	public async Task ExistingFolderGroupOutsideGroupingRoot_WhenReEvaluated_IsNotMarkedDeleted() {
		var dbFactory = this.CreateSqliteDbFactory();
		var groupDir = Path.Combine(this._tempRoot, "ExistingGroup");
		var newFile = this.CreateRealFile(Path.Combine("ExistingGroup", "added.jpg"), "photo");

		await using (var db = await dbFactory.CreateDbContextAsync()) {
			db.MediaItems.Add(CreateMediaItemEntity(1, groupDir, MediaType.FolderGroup, fileSize: 0));
			await db.SaveChangesAsync();
		}

		using var tracker = new FileChangeTracker(dbFactory, NullLogger<FileChangeTracker>.Instance, this.CreateTestConfig((this._tempRoot, false)));
		tracker.OnCreated(groupDir);
		await tracker.ProcessPendingChangesAsync();

		tracker.UnprocessedChanges.Count.ShouldBe(1);
		tracker.UnprocessedChanges[0].ChangeType.ShouldBe(FileChangeType.Added);
		tracker.UnprocessedChanges[0].NewPath.ShouldBe(newFile);
	}

	[Fact]
	public async Task RemoveItems_ExactMatchOnly_DoesNotRemoveSubsequentPathChangeOfSameMediaItemId() {
		var dbFactory = this.CreateSqliteDbFactory();
		var pathA = Path.Combine(this._tempRoot, "A.jpg");
		var pathB = this.CreateRealFile("B.jpg", "same-item");
		var pathC = Path.Combine(this._tempRoot, "C.jpg");

		await using (var db = await dbFactory.CreateDbContextAsync()) {
			db.MediaItems.Add(CreateMediaItemEntity(1, pathA, preHash: FileHashUtility.ComputeFileHash(pathB)));
			await db.SaveChangesAsync();
		}

		using var tracker = new FileChangeTracker(dbFactory, NullLogger<FileChangeTracker>.Instance, this.CreateTestConfig((this._tempRoot, false)));
		tracker.OnRenamed(pathA, pathB);
		await tracker.ProcessPendingChangesAsync();

		var itemAtoB = tracker.UnprocessedChanges.Single();

		File.Move(pathB, pathC);
		tracker.OnRenamed(pathB, pathC);
		await tracker.ProcessPendingChangesAsync();

		// 古い A -> B だけを RemoveItems で除外しても、最新の A -> C は消えない
		tracker.RemoveItems([itemAtoB]);

		tracker.UnprocessedChanges.Count.ShouldBe(1);
		tracker.UnprocessedChanges[0].MediaItemId.ShouldBe(1L);
		tracker.UnprocessedChanges[0].ChangeType.ShouldBe(FileChangeType.Renamed);
		tracker.UnprocessedChanges[0].OldPath.ShouldBe(pathA);
		tracker.UnprocessedChanges[0].NewPath.ShouldBe(pathC);
	}

	[Fact]
	public async Task NotifyChangesApplied_WhenRechangedBeforeNotify_PreservesNewChangeAndUpdatesOldPath() {
		var dbFactory = this.CreateSqliteDbFactory();
		var pathA = Path.Combine(this._tempRoot, "OrigA.jpg");
		var pathB = this.CreateRealFile("MidB.jpg", "same-item");
		var pathC = Path.Combine(this._tempRoot, "FinalC.jpg");

		await using (var db = await dbFactory.CreateDbContextAsync()) {
			db.MediaItems.Add(CreateMediaItemEntity(1, pathA, preHash: FileHashUtility.ComputeFileHash(pathB)));
			await db.SaveChangesAsync();
		}

		using var tracker = new FileChangeTracker(dbFactory, NullLogger<FileChangeTracker>.Instance, this.CreateTestConfig((this._tempRoot, false)));
		tracker.OnRenamed(pathA, pathB);
		await tracker.ProcessPendingChangesAsync();
		var appliedItemAtoB = tracker.UnprocessedChanges.Single();

		// 反映中に実体が B -> C に変わり、先に Reconcile されてリスト上が A -> C に更新された状況
		File.Move(pathB, pathC);
		tracker.OnRenamed(pathB, pathC);
		await tracker.ProcessPendingChangesAsync();
		tracker.UnprocessedChanges.Single().NewPath.ShouldBe(pathC);

		// DB が A -> B へ更新されて NotifyChangesApplied が呼ばれても、C への変更は残る（OldPath も B へ更新）
		tracker.NotifyChangesApplied([appliedItemAtoB], new Dictionary<long, string> { [1] = pathB }, []);

		tracker.UnprocessedChanges.Count.ShouldBe(1);
		var remaining = tracker.UnprocessedChanges[0];
		remaining.MediaItemId.ShouldBe(1L);
		remaining.ChangeType.ShouldBe(FileChangeType.Renamed);
		remaining.OldPath.ShouldBe(pathB);
		remaining.NewPath.ShouldBe(pathC);
	}

	[Fact]
	public async Task NotifyChangesApplied_ConcurrentWithReconcileReadingOldDb_RetriesAndPreservesBToC() {
		var connection = new SqliteConnection("DataSource=:memory:");
		connection.Open();
		this._connections.Add(connection);

		var plainOptions = new DbContextOptionsBuilder<MediaDeckDbContext>()
			.UseSqlite(connection)
			.Options;
		using (var initDb = new MediaDeckDbContext(plainOptions)) {
			initDb.Database.EnsureCreated();
		}

		var pathA = Path.Combine(this._tempRoot, "RaceA.jpg");
		var pathB = Path.Combine(this._tempRoot, "RaceB.jpg");
		var pathC = this.CreateRealFile("RaceC.jpg", "race-content");

		await using (var db = new MediaDeckDbContext(plainOptions)) {
			db.MediaItems.Add(CreateMediaItemEntity(1, pathA, preHash: FileHashUtility.ComputeFileHash(pathC)));
			await db.SaveChangesAsync();
		}

		var pausedOnSecondQuery = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var resumeSecondQuery = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var interceptor = new PauseOnSecondSelectInterceptor(pausedOnSecondQuery, resumeSecondQuery);

		var interceptedOptions = new DbContextOptionsBuilder<MediaDeckDbContext>()
			.UseSqlite(connection)
			.AddInterceptors(interceptor)
			.Options;
		var dbFactoryMock = new Mock<IDbContextFactory<MediaDeckDbContext>>();
		dbFactoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(() => new MediaDeckDbContext(interceptedOptions));

		using var tracker = new FileChangeTracker(dbFactoryMock.Object, NullLogger<FileChangeTracker>.Instance, this.CreateTestConfig((this._tempRoot, false)));
		var appliedItemAtoB = new FileChangeItem {
			MediaItemId = 1,
			ChangeType = FileChangeType.Renamed,
			OldPath = pathA,
			NewPath = pathB
		};
		tracker.UnprocessedChanges.Add(appliedItemAtoB);

		// B -> C の Reconcile を開始（DB読み取り1回目で A を取得した直後、2回目の問い合わせで停止させる）
		tracker.OnRenamed(pathB, pathC);
		var reconcileTask = tracker.ProcessPendingChangesAsync();

		await pausedOnSecondQuery.Task.WaitAsync(TimeSpan.FromSeconds(5));

		// Reconcile が古い DB (A) を抱えている間に、A -> B の DB 反映が完了する
		await using (var db = new MediaDeckDbContext(plainOptions)) {
			var entity = await db.MediaItems.FindAsync(1L);
			entity!.FilePath = pathB;
			entity.DirectoryPath = this._tempRoot;
			await db.SaveChangesAsync();
		}
		tracker.NotifyChangesApplied([appliedItemAtoB], new Dictionary<long, string> { [1] = pathB }, []);

		// Reconcile を再開すると、DB変更世代ズレを検知して再評価し B -> C を保持する
		resumeSecondQuery.SetResult();
		await reconcileTask;

		tracker.UnprocessedChanges.Count.ShouldBe(1);
		var item = tracker.UnprocessedChanges[0];
		item.MediaItemId.ShouldBe(1L);
		item.ChangeType.ShouldBe(FileChangeType.Renamed);
		item.OldPath.ShouldBe(pathB);
		item.NewPath.ShouldBe(pathC);
	}

	[Fact]
	public async Task NotifyChangesApplied_ConcurrentDeleteDuringReconcile_DoesNotResurrectDeletedId() {
		var connection = new SqliteConnection("DataSource=:memory:");
		connection.Open();
		this._connections.Add(connection);

		var plainOptions = new DbContextOptionsBuilder<MediaDeckDbContext>()
			.UseSqlite(connection)
			.Options;
		using (var initDb = new MediaDeckDbContext(plainOptions)) {
			initDb.Database.EnsureCreated();
		}

		var pathA = Path.Combine(this._tempRoot, "DelRaceA.jpg");
		var pathB = Path.Combine(this._tempRoot, "DelRaceB.jpg");
		var extraFile = this.CreateRealFile("DelRaceExtra.jpg", "extra");

		await using (var db = new MediaDeckDbContext(plainOptions)) {
			db.MediaItems.Add(CreateMediaItemEntity(1, pathA, preHash: "hash-del"));
			await db.SaveChangesAsync();
		}

		var pausedOnSecondQuery = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var resumeSecondQuery = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var interceptor = new PauseOnSecondSelectInterceptor(pausedOnSecondQuery, resumeSecondQuery);

		var interceptedOptions = new DbContextOptionsBuilder<MediaDeckDbContext>()
			.UseSqlite(connection)
			.AddInterceptors(interceptor)
			.Options;
		var dbFactoryMock = new Mock<IDbContextFactory<MediaDeckDbContext>>();
		dbFactoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(() => new MediaDeckDbContext(interceptedOptions));

		using var tracker = new FileChangeTracker(dbFactoryMock.Object, NullLogger<FileChangeTracker>.Instance, this.CreateTestConfig((this._tempRoot, false)));
		var deletedItem = new FileChangeItem {
			MediaItemId = 1,
			ChangeType = FileChangeType.Deleted,
			OldPath = pathA
		};
		tracker.UnprocessedChanges.Add(deletedItem);

		tracker.OnRenamed(pathA, pathB);
		tracker.OnCreated(extraFile);
		var reconcileTask = tracker.ProcessPendingChangesAsync();
		await pausedOnSecondQuery.Task.WaitAsync(TimeSpan.FromSeconds(5));

		await using (var db = new MediaDeckDbContext(plainOptions)) {
			var entity = await db.MediaItems.FindAsync(1L);
			db.MediaItems.Remove(entity!);
			await db.SaveChangesAsync();
		}
		tracker.NotifyChangesApplied([deletedItem], new Dictionary<long, string>(), [1L]);

		resumeSecondQuery.SetResult();
		await reconcileTask;

		tracker.UnprocessedChanges.ShouldNotContain(x => x.MediaItemId == 1L);
		tracker.UnprocessedChanges.ShouldContain(x => x.ChangeType == FileChangeType.Added && x.NewPath == extraFile);
	}

	[Fact]
	public async Task MassUntrackedDeletedFiles_CompletesCleanlyWithoutFalsePositives() {
		var dbFactory = this.CreateSqliteDbFactory();
		await using (var db = await dbFactory.CreateDbContextAsync()) {
			for (int i = 0; i < 200; i++) {
				db.MediaItems.Add(CreateMediaItemEntity(i + 1, Path.Combine(this._tempRoot, $"kept_{i}.jpg"), preHash: $"h{i}"));
			}
			await db.SaveChangesAsync();
		}

		using var tracker = new FileChangeTracker(dbFactory, NullLogger<FileChangeTracker>.Instance, this.CreateTestConfig((this._tempRoot, false)));
		for (int i = 0; i < 200; i++) {
			tracker.OnDeleted(Path.Combine(this._tempRoot, $"untracked_{i}.tmp"));
		}

		await tracker.ProcessPendingChangesAsync();

		tracker.UnprocessedChanges.ShouldBeEmpty();
	}

	[Fact]
	public async Task Dispose_WhileReconcileInFlight_DoesNotThrow() {
		var dbFactory = this.CreateSqliteDbFactory();
		var enteredDbFactory = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var releaseDbFactory = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

		var slowFactoryMock = new Mock<IDbContextFactory<MediaDeckDbContext>>();
		slowFactoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
			.Returns(async (CancellationToken _) => {
				enteredDbFactory.TrySetResult();
				await releaseDbFactory.Task;
				return await dbFactory.CreateDbContextAsync();
			});

		var tracker = new FileChangeTracker(slowFactoryMock.Object, NullLogger<FileChangeTracker>.Instance, this.CreateTestConfig((this._tempRoot, false)));
		tracker.OnDeleted(Path.Combine(this._tempRoot, "gone.jpg"));

		var reconcileTask = tracker.ProcessPendingChangesAsync();
		await enteredDbFactory.Task.WaitAsync(TimeSpan.FromSeconds(5));

		tracker.Dispose();
		tracker.Dispose();
		releaseDbFactory.SetResult();

		await Should.NotThrowAsync(async () => await reconcileTask);
	}

	/// <summary>
	/// 単一バッチ内で上書き移動の連鎖（A -&gt; B 後に B -&gt; C）が発生した場合に、
	/// 消滅した B が C への重複 Renamed に化けず Deleted として維持されることを検証します。
	/// </summary>
	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task OverwriteMoveChain_SameBatch_DetectsWinnerRenamedAndOverwrittenDeleted(bool withExplicitDeleteEvent) {
		var dbFactory = this.CreateSqliteDbFactory();
		var pathA = Path.Combine(this._tempRoot, "ChainOverA.jpg");
		var pathB = Path.Combine(this._tempRoot, "ChainOverB.jpg");
		var pathC = this.CreateRealFile("ChainOverC.jpg", "origin-a-content");

		await using (var db = await dbFactory.CreateDbContextAsync()) {
			db.MediaItems.AddRange(
				CreateMediaItemEntity(1, pathA, preHash: FileHashUtility.ComputeFileHash(pathC), fileSize: new FileInfo(pathC).Length),
				CreateMediaItemEntity(2, pathB, preHash: "origin-b-hash", fileSize: 999));
			await db.SaveChangesAsync();
		}

		using var tracker = new FileChangeTracker(dbFactory, NullLogger<FileChangeTracker>.Instance, this.CreateTestConfig((this._tempRoot, false)));
		if (withExplicitDeleteEvent) {
			tracker.OnDeleted(pathB);
		}
		tracker.OnRenamed(pathA, pathB);
		tracker.OnRenamed(pathB, pathC);
		await tracker.ProcessPendingChangesAsync();

		tracker.UnprocessedChanges.Count.ShouldBe(2);
		tracker.UnprocessedChanges.ShouldContain(x =>
			x.MediaItemId == 1
			&& x.ChangeType == FileChangeType.Renamed
			&& x.OldPath == pathA
			&& x.NewPath == pathC);
		tracker.UnprocessedChanges.ShouldContain(x =>
			x.MediaItemId == 2
			&& x.ChangeType == FileChangeType.Deleted
			&& x.OldPath == pathB);
	}

	/// <summary>
	/// クロスバッチで上書き移動（Renamed A -&gt; B / Deleted B）が確定した後、さらに B -&gt; C へリネームされても、
	/// 既に Deleted の B が C への重複 Renamed へ復活しないことを検証します。
	/// </summary>
	[Fact]
	public async Task OverwriteMoveChain_AcrossBatches_DoesNotResurrectDeletedItemAsDuplicateRenamed() {
		var dbFactory = this.CreateSqliteDbFactory();
		var pathA = Path.Combine(this._tempRoot, "CrossOverA.jpg");
		var pathB = this.CreateRealFile("CrossOverB.jpg", "origin-a-content");
		var pathC = Path.Combine(this._tempRoot, "CrossOverC.jpg");

		await using (var db = await dbFactory.CreateDbContextAsync()) {
			db.MediaItems.AddRange(
				CreateMediaItemEntity(1, pathA, preHash: FileHashUtility.ComputeFileHash(pathB), fileSize: new FileInfo(pathB).Length),
				CreateMediaItemEntity(2, pathB, preHash: "origin-b-hash", fileSize: 999));
			await db.SaveChangesAsync();
		}

		using var tracker = new FileChangeTracker(dbFactory, NullLogger<FileChangeTracker>.Instance, this.CreateTestConfig((this._tempRoot, false)));

		// Batch 1: B を削除して A -> B
		tracker.OnDeleted(pathB);
		tracker.OnRenamed(pathA, pathB);
		await tracker.ProcessPendingChangesAsync();

		tracker.UnprocessedChanges.Count.ShouldBe(2);
		tracker.UnprocessedChanges.ShouldContain(x => x.MediaItemId == 1 && x.ChangeType == FileChangeType.Renamed && x.NewPath == pathB);
		tracker.UnprocessedChanges.ShouldContain(x => x.MediaItemId == 2 && x.ChangeType == FileChangeType.Deleted && x.OldPath == pathB);

		// Batch 2: B -> C
		File.Move(pathB, pathC);
		tracker.OnRenamed(pathB, pathC);
		await tracker.ProcessPendingChangesAsync();

		tracker.UnprocessedChanges.Count.ShouldBe(2);
		tracker.UnprocessedChanges.ShouldContain(x =>
			x.MediaItemId == 1
			&& x.ChangeType == FileChangeType.Renamed
			&& x.OldPath == pathA
			&& x.NewPath == pathC);
		tracker.UnprocessedChanges.ShouldContain(x =>
			x.MediaItemId == 2
			&& x.ChangeType == FileChangeType.Deleted
			&& x.OldPath == pathB);
	}

	/// <summary>
	/// Atomic Save（一時ファイルから既存ファイルへの置換）の直後に同一バッチでリネームされた場合でも、
	/// 登録済みアイテムが誤って消失扱いにならず Renamed として維持されることを検証します。
	/// </summary>
	[Fact]
	public async Task AtomicSaveFollowedByRename_SameBatch_PreservesMediaItemAsRenamed() {
		var dbFactory = this.CreateSqliteDbFactory();
		var pathTmp = Path.Combine(this._tempRoot, "photo.jpg.tmp");
		var pathA = Path.Combine(this._tempRoot, "photo.jpg");
		var pathB = this.CreateRealFile("photo_renamed.jpg", "updated-image-bytes");

		await using (var db = await dbFactory.CreateDbContextAsync()) {
			db.MediaItems.Add(CreateMediaItemEntity(1, pathA, preHash: "old-hash", fileSize: 100));
			await db.SaveChangesAsync();
		}

		using var tracker = new FileChangeTracker(dbFactory, NullLogger<FileChangeTracker>.Instance, this.CreateTestConfig((this._tempRoot, false)));
		tracker.OnCreated(pathTmp);
		tracker.OnRenamed(pathTmp, pathA);
		tracker.OnRenamed(pathA, pathB);
		await tracker.ProcessPendingChangesAsync();

		tracker.UnprocessedChanges.Count.ShouldBe(1);
		var item = tracker.UnprocessedChanges[0];
		item.MediaItemId.ShouldBe(1L);
		item.ChangeType.ShouldBe(FileChangeType.Renamed);
		item.OldPath.ShouldBe(pathA);
		item.NewPath.ShouldBe(pathB);
	}

	/// <summary>
	/// <see cref="MediaType.Unknown"/> として登録された拡張子設定は監視対象ファイルとして扱われないことを検証します。
	/// </summary>
	[Fact]
	public async Task ExtensionFiltering_IgnoresUnknownMediaTypeExtension() {
		var dbFactory = this.CreateSqliteDbFactory();
		var config = this.CreateTestConfig((this._tempRoot, false));
		var unknownExt = new ExtensionObjectModel();
		unknownExt.Extension.Value = ".customunknown";
		unknownExt.MediaType.Value = MediaType.Unknown;
		config.ScanConfig.TargetExtensions.Add(unknownExt);

		var unknownFile = this.CreateRealFile("ignored.customunknown", "ignored-content");
		var targetFile = this.CreateRealFile("valid.jpg", "valid-content");

		using var tracker = new FileChangeTracker(dbFactory, NullLogger<FileChangeTracker>.Instance, config);
		tracker.OnCreated(unknownFile);
		tracker.OnCreated(targetFile);
		await tracker.ProcessPendingChangesAsync();

		tracker.UnprocessedChanges.Count.ShouldBe(1);
		tracker.UnprocessedChanges[0].ChangeType.ShouldBe(FileChangeType.Added);
		tracker.UnprocessedChanges[0].NewPath.ShouldBe(targetFile);
	}

	/// <summary>
	/// 差分計算の実行中に別スレッドから設定コレクションが変更されても例外が発生しないことを検証します。
	/// </summary>
	[Fact]
	public async Task Reconcile_ConcurrentConfigModification_DoesNotThrow() {
		var dbFactory = this.CreateSqliteDbFactory();
		var config = this.CreateTestConfig((this._tempRoot, false));
		var targetFile = this.CreateRealFile("concurrent_cfg.jpg", "content");

		using var tracker = new FileChangeTracker(dbFactory, NullLogger<FileChangeTracker>.Instance, config);
		using var cts = new CancellationTokenSource();

		var modifierTask = Task.Run(() => {
			var toggle = 0;
			while (!cts.Token.IsCancellationRequested) {
				var ext = new ExtensionObjectModel();
				ext.Extension.Value = $".ext{toggle++ % 5}";
				ext.MediaType.Value = MediaType.Image;
				config.ScanConfig.TargetExtensions.Add(ext);
				config.ScanConfig.TargetExtensions.Remove(ext);

				var folder = new FolderModel { FolderPath = Path.Combine(this._tempRoot, $"dyn_{toggle % 3}"), IsGroupingRoot = true };
				config.FolderManagerConfig.Folders.Add(folder);
				config.FolderManagerConfig.Folders.Remove(folder);
			}
		});

		try {
			for (var i = 0; i < 30; i++) {
				tracker.OnCreated(targetFile);
				await Should.NotThrowAsync(() => tracker.ProcessPendingChangesAsync());
			}
		} finally {
			cts.Cancel();
			await modifierTask;
		}

		tracker.UnprocessedChanges.ShouldContain(x => x.ChangeType == FileChangeType.Added && x.NewPath == targetFile);
	}

	/// <summary>
	/// ファイルロックによるリトライ上限到達時に、実在ファイルはハッシュなし Added へフォールバックし、
	/// 既に削除されたファイルからは幽霊 Added が生成されないことを検証します。
	/// </summary>
	[Fact]
	public async Task LockedFile_RetryLimitFallback_HandlesExistingAndDeletedFilesAccurately() {
		var dbFactory = this.CreateSqliteDbFactory();
		var persistentLockedPath = this.CreateRealFile("still_locked.jpg", "locked-12345");
		var vanishedPath = this.CreateRealFile("vanished_after_lock.jpg", "temp-content");

		using var tracker = new FileChangeTracker(dbFactory, NullLogger<FileChangeTracker>.Instance, this.CreateTestConfig((this._tempRoot, false)));
		tracker.StabilityDelay = TimeSpan.FromSeconds(30);

		using var persistentLock = new FileStream(persistentLockedPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
		using (var vanishedLock = new FileStream(vanishedPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) {
			tracker.OnCreated(persistentLockedPath);
			tracker.OnCreated(vanishedPath);

			// リトライ上限（5回）をすべて消費する
			for (var i = 0; i < 5; i++) {
				await tracker.ProcessPendingChangesAsync();
				tracker.UnprocessedChanges.ShouldBeEmpty();
			}
		}

		// 6回目実行前に vanishedPath を削除する
		File.Delete(vanishedPath);
		await tracker.ProcessPendingChangesAsync();

		tracker.UnprocessedChanges.Count.ShouldBe(1);
		var added = tracker.UnprocessedChanges[0];
		added.ChangeType.ShouldBe(FileChangeType.Added);
		added.NewPath.ShouldBe(persistentLockedPath);
		added.NewHash.ShouldBeEmpty();
	}

	/// <summary>
	/// 反映中にラウンドトリップ（B -&gt; A）または削除後の再作成でリスト上から相殺消去された後、
	/// DB 更新完了の <see cref="FileChangeTracker.NotifyChangesApplied"/> が呼ばれた際に再評価がスケジュールされることを検証します。
	/// </summary>
	[Fact]
	public async Task NotifyChangesApplied_WhenCancelledOutByRoundTripOrRecreateBeforeNotify_ReschedulesAndRedetectsDiff() {
		var dbFactory = this.CreateSqliteDbFactory();
		var pathA = this.CreateRealFile("RoundA.jpg", "roundtrip-content");
		var pathB = Path.Combine(this._tempRoot, "RoundB.jpg");
		var recreatedPath = this.CreateRealFile("Recreated.jpg", "recreated-content");
		var hashA = FileHashUtility.ComputeFileHash(pathA);

		await using (var db = await dbFactory.CreateDbContextAsync()) {
			db.MediaItems.AddRange(
				CreateMediaItemEntity(1, pathB, preHash: hashA, fileSize: new FileInfo(pathA).Length),
				CreateMediaItemEntity(2, recreatedPath, preHash: "old-hash"));
			await db.SaveChangesAsync();
		}

		using var tracker = new FileChangeTracker(dbFactory, NullLogger<FileChangeTracker>.Instance, this.CreateTestConfig((this._tempRoot, false)));
		tracker.StabilityDelay = TimeSpan.FromSeconds(30);

		// ケース1: UnprocessedChanges から A -> B が既に相殺消去された後、DB が B へ更新された通知が来る
		var appliedRename = new FileChangeItem { MediaItemId = 1, ChangeType = FileChangeType.Renamed, OldPath = pathA, NewPath = pathB };
		tracker.NotifyChangesApplied([appliedRename], new Dictionary<long, string> { [1] = pathB }, []);
		await tracker.ProcessPendingChangesAsync();

		tracker.UnprocessedChanges.ShouldContain(x =>
			x.MediaItemId == 1
			&& x.ChangeType == FileChangeType.Moved
			&& x.OldPath == pathB
			&& x.NewPath == pathA);

		// ケース2: UnprocessedChanges から Deleted(2) が既に相殺消去された後、DB から id=2 が削除された通知が来る
		await using (var db = await dbFactory.CreateDbContextAsync()) {
			var entity = await db.MediaItems.FindAsync(2L);
			db.MediaItems.Remove(entity!);
			await db.SaveChangesAsync();
		}
		var appliedDelete = new FileChangeItem { MediaItemId = 2, ChangeType = FileChangeType.Deleted, OldPath = recreatedPath };
		tracker.NotifyChangesApplied([appliedDelete], new Dictionary<long, string>(), [2L]);
		await tracker.ProcessPendingChangesAsync();

		tracker.UnprocessedChanges.ShouldContain(x =>
			x.MediaItemId == null
			&& x.ChangeType == FileChangeType.Added
			&& x.NewPath == recreatedPath);
	}

	private sealed class PauseOnSecondSelectInterceptor(TaskCompletionSource paused, TaskCompletionSource resume) : DbCommandInterceptor {
		private int _selectCount;
		private bool _armed = true;

		public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
			DbCommand command,
			CommandEventData eventData,
			InterceptionResult<DbDataReader> result,
			CancellationToken cancellationToken = default) {
			if (this._armed && Interlocked.Increment(ref this._selectCount) == 2) {
				this._armed = false;
				paused.TrySetResult();
				await resume.Task;
			}
			return await base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
		}
	}
}