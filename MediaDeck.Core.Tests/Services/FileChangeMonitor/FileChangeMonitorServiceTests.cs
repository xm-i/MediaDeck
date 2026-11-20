using MediaDeck.Common.Utilities;
using MediaDeck.Composition.Database;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.Services;
using MediaDeck.Composition.Objects;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Composition.Stores.Config.Model.Objects;
using MediaDeck.Composition.Stores.State.Model.Objects;
using MediaDeck.Composition.Tables;
using MediaDeck.Core.Models.Files;
using MediaDeck.Core.Models.NotificationDispatcher;
using MediaDeck.Core.Services.FileChangeMonitor;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Moq;

using R3;

using Shouldly;

namespace MediaDeck.Core.Tests.Services.FileChangeMonitor;

/// <summary>
/// <see cref="FileChangeMonitorService"/> の単体テストクラスです。
/// </summary>
public class FileChangeMonitorServiceTests : IDisposable {
	private readonly SqliteConnection _connection;
	private readonly DbContextOptions<MediaDeckDbContext> _options;
	private readonly Mock<IDbContextFactory<MediaDeckDbContext>> _dbFactoryMock;
	private readonly ServiceProvider _serviceProvider;
	private readonly ConfigModel _config;
	private readonly IStringProvider _stringProvider;
	private readonly AppNotificationDispatcher _appNotifDispatcher;
	private readonly FileRegistrar _fileRegistrar;
	private readonly FileChangeTracker _tracker;
	private readonly FileChangeMonitorService _sut;
	private readonly List<string> _registeredPaths = [];
	private readonly string _tempRoot;

	/// <summary>
	/// <see cref="FileChangeMonitorServiceTests"/> の新しいインスタンスを初期化します。
	/// </summary>
	public FileChangeMonitorServiceTests() {
		this._tempRoot = Path.Combine(Path.GetTempPath(), $"MediaDeck_FCMS_{Guid.NewGuid():N}");
		Directory.CreateDirectory(this._tempRoot);

		this._connection = new SqliteConnection("DataSource=:memory:");
		this._connection.Open();
		this._options = new DbContextOptionsBuilder<MediaDeckDbContext>()
			.UseSqlite(this._connection)
			.Options;
		using (var initDb = new MediaDeckDbContext(this._options)) {
			initDb.Database.EnsureCreated();
		}

		this._dbFactoryMock = new Mock<IDbContextFactory<MediaDeckDbContext>>();
		this._dbFactoryMock.Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(() => new MediaDeckDbContext(this._options));

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
		this._serviceProvider = services.BuildServiceProvider();
		this._config = this._serviceProvider.GetRequiredService<ConfigModel>();

		var trackerLogger = new Mock<ILogger<FileChangeTracker>>();
		this._tracker = new FileChangeTracker(this._dbFactoryMock.Object, trackerLogger.Object, this._config);

		var logger = new Mock<ILogger<FileChangeMonitorService>>();
		this._appNotifDispatcher = new AppNotificationDispatcher();
		var dbWriteCoord = new Mock<IDatabaseWriteCoordinator>();
		dbWriteCoord.Setup(x => x.ExecuteAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
			.Returns<Func<CancellationToken, Task>, CancellationToken>(async (op, ct) => await op(ct));

		var unknownFactoryMock = new Mock<IMediaItemFactory>();
		unknownFactoryMock.Setup(x => x.MediaType).Returns(MediaType.Unknown);

		var mediaItemTypeService = new Mock<IMediaItemTypeService>();
		mediaItemTypeService.Setup(x => x.CreateMediaItemOperators()).Returns([]);
		mediaItemTypeService.Setup(x => x.GetMediaItemFactory(It.IsAny<string>()))
			.Callback<string>(path => {
				lock (this._registeredPaths) {
					this._registeredPaths.Add(path);
				}
			})
			.Returns(unknownFactoryMock.Object);

		this._fileRegistrar = new FileRegistrar(this._config, new Mock<ILogger<FileRegistrar>>().Object, mediaItemTypeService.Object);
		this._stringProvider = this._serviceProvider.GetRequiredService<IStringProvider>();
		this._sut = new FileChangeMonitorService(this._dbFactoryMock.Object, this._tracker, logger.Object, this._appNotifDispatcher, this._fileRegistrar, dbWriteCoord.Object, this._stringProvider);
	}

	/// <inheritdoc />
	public void Dispose() {
		this._sut.Dispose();
		this._fileRegistrar.Dispose();
		this._serviceProvider.Dispose();
		this._connection.Dispose();
		if (Directory.Exists(this._tempRoot)) {
			try {
				Directory.Delete(this._tempRoot, recursive: true);
			} catch {
				// クリーンアップ失敗は無視
			}
		}
	}

	private async Task WaitForRegisteredCountAsync(int expectedCount) {
		var deadline = DateTime.UtcNow.AddSeconds(5);
		while (DateTime.UtcNow < deadline) {
			lock (this._registeredPaths) {
				if (this._registeredPaths.Count >= expectedCount) {
					return;
				}
			}
			await Task.Delay(25);
		}
	}

	[Fact]
	public void Tracker_IsNotNull() {
		this._sut.Tracker.ShouldNotBeNull();
	}

	[Fact]
	public void DiscardChanges_RemovesItemsFromTracker() {
		var item = new FileChangeItem { ChangeType = FileChangeType.Added, NewPath = "test.jpg" };
		this._tracker.UnprocessedChanges.Add(item);

		this._sut.DiscardChanges([item]);

		this._tracker.UnprocessedChanges.ShouldNotContain(item);
	}

	[Fact]
	public async Task ApplyChangesAsync_AddedItem_EnqueuesInRegistrar() {
		var item = new FileChangeItem { ChangeType = FileChangeType.Added, NewPath = "new.jpg" };
		this._tracker.UnprocessedChanges.Add(item);

		await this._sut.ApplyChangesAsync([item], false);
		await this.WaitForRegisteredCountAsync(1);

		this._tracker.UnprocessedChanges.ShouldNotContain(item);
		lock (this._registeredPaths) {
			this._registeredPaths.ShouldBe(["new.jpg"]);
		}
	}

	[Fact]
	public async Task ApplyChangesAsync_MovedItem_UpdatesFilePathAndDirectoryPath() {
		var oldDir = Path.GetFullPath("OldFolder");
		var newDir = Path.GetFullPath("NewFolder");
		var oldPath = Path.Combine(oldDir, "image.png");
		var newPath = Path.Combine(newDir, "image.png");

		await using (var db = new MediaDeckDbContext(this._options)) {
			db.MediaItems.Add(new MediaItem {
				MediaItemId = 100,
				MediaType = MediaType.Image,
				FilePath = oldPath,
				DirectoryPath = oldDir,
				Description = string.Empty,
				IsUnderFolderGroup = false,
			});
			await db.SaveChangesAsync();
		}

		var item = new FileChangeItem {
			ChangeType = FileChangeType.Moved,
			MediaItemId = 100,
			OldPath = oldPath,
			NewPath = newPath,
		};
		this._tracker.UnprocessedChanges.Add(item);

		await this._sut.ApplyChangesAsync([item], false);

		await using (var db = new MediaDeckDbContext(this._options)) {
			var updated = await db.MediaItems.FindAsync(100L);
			updated.ShouldNotBeNull();
			updated.FilePath.ShouldBe(newPath);
			updated.DirectoryPath.ShouldBe(newDir);
		}
	}

	[Fact]
	public async Task ApplyChangesAsync_CreatedThenRenamed_EnqueuesFinalRenamedPathInRegistrar() {
		var initialPath = Path.Combine(this._tempRoot, "initial.jpg");
		var renamedPath = Path.Combine(this._tempRoot, "final_name.jpg");
		File.WriteAllText(renamedPath, "image-bytes");

		this._tracker.OnCreated(initialPath);
		this._tracker.OnRenamed(initialPath, renamedPath);
		await this._tracker.ProcessPendingChangesAsync();

		this._tracker.UnprocessedChanges.Count.ShouldBe(1);
		await this._sut.ApplyChangesAsync(this._tracker.UnprocessedChanges.ToArray(), false);
		await this.WaitForRegisteredCountAsync(1);

		this._tracker.UnprocessedChanges.ShouldBeEmpty();
		lock (this._registeredPaths) {
			this._registeredPaths.ShouldBe([renamedPath]);
		}
	}

	[Fact]
	public async Task ApplyChangesAsync_MovingFilesIntoAndOutOfFolderGroup_UpdatesIsUnderFolderGroup() {
		var groupDir = Path.Combine(this._tempRoot, "AlbumGroup");
		var outsideDir = Path.Combine(this._tempRoot, "Loose");
		var fileEnteringGroupOld = Path.Combine(outsideDir, "enter.jpg");
		var fileEnteringGroupNew = Path.Combine(groupDir, "enter.jpg");
		var fileLeavingGroupOld = Path.Combine(groupDir, "leave.jpg");
		var fileLeavingGroupNew = Path.Combine(outsideDir, "leave.jpg");

		await using (var db = new MediaDeckDbContext(this._options)) {
			db.MediaItems.AddRange(
				new MediaItem {
					MediaItemId = 1,
					MediaType = MediaType.FolderGroup,
					FilePath = groupDir,
					DirectoryPath = this._tempRoot,
					Description = string.Empty,
					IsUnderFolderGroup = false,
				},
				new MediaItem {
					MediaItemId = 2,
					MediaType = MediaType.Image,
					FilePath = fileEnteringGroupOld,
					DirectoryPath = outsideDir,
					Description = string.Empty,
					IsUnderFolderGroup = false,
				},
				new MediaItem {
					MediaItemId = 3,
					MediaType = MediaType.Image,
					FilePath = fileLeavingGroupOld,
					DirectoryPath = groupDir,
					Description = string.Empty,
					IsUnderFolderGroup = true,
				});
			await db.SaveChangesAsync();
		}

		var changes = new[] {
			new FileChangeItem { MediaItemId = 2, ChangeType = FileChangeType.Moved, OldPath = fileEnteringGroupOld, NewPath = fileEnteringGroupNew },
			new FileChangeItem { MediaItemId = 3, ChangeType = FileChangeType.Moved, OldPath = fileLeavingGroupOld, NewPath = fileLeavingGroupNew }
		};
		this._tracker.UnprocessedChanges.AddRange(changes);

		await this._sut.ApplyChangesAsync(changes, false);

		await using (var db = new MediaDeckDbContext(this._options)) {
			var entered = await db.MediaItems.FindAsync(2L);
			var left = await db.MediaItems.FindAsync(3L);
			entered!.IsUnderFolderGroup.ShouldBeTrue();
			entered.FilePath.ShouldBe(fileEnteringGroupNew);
			left!.IsUnderFolderGroup.ShouldBeFalse();
			left.FilePath.ShouldBe(fileLeavingGroupNew);
		}
	}

	[Fact]
	public async Task ApplyChangesAsync_DeletedFolderGroup_UpdatesChildItemsIsUnderFolderGroupToFalse() {
		var groupDir = Path.Combine(this._tempRoot, "RemovedGroup");
		var childFile = Path.Combine(groupDir, "Sub", "pic.jpg");

		await using (var db = new MediaDeckDbContext(this._options)) {
			db.MediaItems.AddRange(
				new MediaItem {
					MediaItemId = 10,
					MediaType = MediaType.FolderGroup,
					FilePath = groupDir,
					DirectoryPath = this._tempRoot,
					Description = string.Empty,
					IsUnderFolderGroup = false,
				},
				new MediaItem {
					MediaItemId = 11,
					MediaType = MediaType.Image,
					FilePath = childFile,
					DirectoryPath = Path.Combine(groupDir, "Sub"),
					Description = string.Empty,
					IsUnderFolderGroup = true,
				});
			await db.SaveChangesAsync();
		}

		var deleteGroup = new FileChangeItem {
			MediaItemId = 10,
			ChangeType = FileChangeType.Deleted,
			OldPath = groupDir,
		};
		this._tracker.UnprocessedChanges.Add(deleteGroup);

		await this._sut.ApplyChangesAsync([deleteGroup], false);

		await using (var db = new MediaDeckDbContext(this._options)) {
			(await db.MediaItems.FindAsync(10L)).ShouldBeNull();
			var child = await db.MediaItems.FindAsync(11L);
			child.ShouldNotBeNull();
			child.IsUnderFolderGroup.ShouldBeFalse();
		}
	}

	[Fact]
	public async Task ApplyChangesAsync_AddedFilesAndFolderGroup_EnqueuesFilesBeforeDirectory() {
		var albumDir = Path.Combine(this._tempRoot, "NewAlbum");
		var fileInAlbum = Path.Combine(albumDir, "01.jpg");
		Directory.CreateDirectory(albumDir);
		File.WriteAllText(fileInAlbum, "photo");

		var items = new[] {
			new FileChangeItem { ChangeType = FileChangeType.Added, NewPath = albumDir },
			new FileChangeItem { ChangeType = FileChangeType.Added, NewPath = fileInAlbum }
		};
		this._tracker.UnprocessedChanges.AddRange(items);

		await this._sut.ApplyChangesAsync(items, false);
		await this.WaitForRegisteredCountAsync(2);

		lock (this._registeredPaths) {
			this._registeredPaths.ShouldBe([fileInAlbum, albumDir]);
		}
	}

	[Fact]
	public async Task ApplyChangesAsync_RenamedFollowedByAddedAtOldPath_UpdatesDbBeforeRegistration() {
		var oldPath = Path.Combine(this._tempRoot, "reuse.jpg");
		var movedPath = Path.Combine(this._tempRoot, "moved.jpg");
		File.WriteAllText(oldPath, "brand-new");
		File.WriteAllText(movedPath, "existing");

		await using (var db = new MediaDeckDbContext(this._options)) {
			db.MediaItems.Add(new MediaItem {
				MediaItemId = 20,
				MediaType = MediaType.Image,
				FilePath = oldPath,
				DirectoryPath = this._tempRoot,
				Description = string.Empty,
				IsUnderFolderGroup = false,
			});
			await db.SaveChangesAsync();
		}

		var items = new[] {
			new FileChangeItem { ChangeType = FileChangeType.Added, NewPath = oldPath },
			new FileChangeItem { MediaItemId = 20, ChangeType = FileChangeType.Renamed, OldPath = oldPath, NewPath = movedPath }
		};
		this._tracker.UnprocessedChanges.AddRange(items);

		var filesChangedNotified = false;
		using var sub = FileNotifications.FilesChanged.Subscribe(_ => filesChangedNotified = true);

		await this._sut.ApplyChangesAsync(items, false);
		await this.WaitForRegisteredCountAsync(1);

		filesChangedNotified.ShouldBeTrue();
		await using (var db = new MediaDeckDbContext(this._options)) {
			var moved = await db.MediaItems.FindAsync(20L);
			moved!.FilePath.ShouldBe(movedPath);
		}
		lock (this._registeredPaths) {
			this._registeredPaths.ShouldBe([oldPath]);
		}
	}

	[Fact]
	public async Task RealFileSystemWatcher_FolderMoveAndCreateThenRename_DetectsAccurately() {
		var folderA = Path.Combine(this._tempRoot, "FolderA");
		var folderB = Path.Combine(this._tempRoot, "FolderB");
		var subA = Path.Combine(folderA, "Sub");
		Directory.CreateDirectory(subA);
		var oldImg = Path.Combine(subA, "img1.jpg");
		File.WriteAllText(oldImg, "existing-image-data");

		await using (var db = new MediaDeckDbContext(this._options)) {
			db.MediaItems.Add(new MediaItem {
				MediaItemId = 50,
				MediaType = MediaType.Image,
				FilePath = oldImg,
				DirectoryPath = subA,
				Description = string.Empty,
				IsExists = true,
				IsUnderFolderGroup = false,
			});
			await db.SaveChangesAsync();
		}

		this._tracker.StabilityDelay = TimeSpan.FromMilliseconds(100);
		this._config.FolderManagerConfig.Folders.Add(new FolderModel {
			FolderPath = this._tempRoot,
			IsGroupingRoot = false
		});

		// 実OS上でフォルダ移動と新規作成→即リネームを実行
		Directory.Move(folderA, folderB);
		var createdTmp = Path.Combine(this._tempRoot, "temp_created.jpg");
		var renamedNew = Path.Combine(this._tempRoot, "final_created.jpg");
		File.WriteAllText(createdTmp, "brand-new-image-data");
		File.Move(createdTmp, renamedNew);

		var expectedMovedImg = Path.Combine(folderB, "Sub", "img1.jpg");
		var deadline = DateTime.UtcNow.AddSeconds(5);
		while (DateTime.UtcNow < deadline) {
			if (this._tracker.UnprocessedChanges.Any(x => x.MediaItemId == 50 && x.NewPath == expectedMovedImg)
				&& this._tracker.UnprocessedChanges.Any(x => x.ChangeType == FileChangeType.Added && x.NewPath == renamedNew)) {
				break;
			}
			await Task.Delay(50);
		}

		this._tracker.UnprocessedChanges.Count.ShouldBe(2);
		this._tracker.UnprocessedChanges.ShouldContain(x =>
			x.MediaItemId == 50
			&& x.ChangeType == FileChangeType.Renamed
			&& x.OldPath == oldImg
			&& x.NewPath == expectedMovedImg);
		this._tracker.UnprocessedChanges.ShouldContain(x =>
			x.MediaItemId == null
			&& x.ChangeType == FileChangeType.Added
			&& x.NewPath == renamedNew);
	}

	[Fact]
	public async Task ApplyChangesAsync_SwapRename_UpdatesBothItemsWithoutUniqueConstraintError() {
		var pathA = Path.Combine(this._tempRoot, "SwapA.jpg");
		var pathB = Path.Combine(this._tempRoot, "SwapB.jpg");
		var pathTmp = Path.Combine(this._tempRoot, "SwapTmp.jpg");
		File.WriteAllText(pathA, "was-b");
		File.WriteAllText(pathB, "was-a");

		await using (var db = new MediaDeckDbContext(this._options)) {
			db.MediaItems.AddRange(
				new MediaItem {
					MediaItemId = 1,
					MediaType = MediaType.Image,
					FilePath = pathA,
					DirectoryPath = this._tempRoot,
					Description = string.Empty,
					IsUnderFolderGroup = false,
				},
				new MediaItem {
					MediaItemId = 2,
					MediaType = MediaType.Image,
					FilePath = pathB,
					DirectoryPath = this._tempRoot,
					Description = string.Empty,
					IsUnderFolderGroup = false,
				});
			await db.SaveChangesAsync();
		}

		this._tracker.OnRenamed(pathA, pathTmp);
		this._tracker.OnRenamed(pathB, pathA);
		this._tracker.OnRenamed(pathTmp, pathB);
		await this._tracker.ProcessPendingChangesAsync();

		this._tracker.UnprocessedChanges.Count.ShouldBe(2);

		await this._sut.ApplyChangesAsync(this._tracker.UnprocessedChanges.ToArray(), false);

		this._tracker.UnprocessedChanges.ShouldBeEmpty();
		await using (var db = new MediaDeckDbContext(this._options)) {
			(await db.MediaItems.FindAsync(1L))!.FilePath.ShouldBe(pathB);
			(await db.MediaItems.FindAsync(2L))!.FilePath.ShouldBe(pathA);
		}
	}

	[Fact]
	public async Task ApplyChangesAsync_ChainedRename_UpdatesAllItemsWithoutUniqueConstraintError() {
		var pathA = Path.Combine(this._tempRoot, "ChainA.jpg");
		var pathB = Path.Combine(this._tempRoot, "ChainB.jpg");
		var pathC = Path.Combine(this._tempRoot, "ChainC.jpg");

		await using (var db = new MediaDeckDbContext(this._options)) {
			db.MediaItems.AddRange(
				new MediaItem {
					MediaItemId = 1,
					MediaType = MediaType.Image,
					FilePath = pathA,
					DirectoryPath = this._tempRoot,
					Description = string.Empty,
					IsUnderFolderGroup = false,
				},
				new MediaItem {
					MediaItemId = 2,
					MediaType = MediaType.Image,
					FilePath = pathB,
					DirectoryPath = this._tempRoot,
					Description = string.Empty,
					IsUnderFolderGroup = false,
				});
			await db.SaveChangesAsync();
		}

		var changes = new[] {
			new FileChangeItem { MediaItemId = 1, ChangeType = FileChangeType.Renamed, OldPath = pathA, NewPath = pathB },
			new FileChangeItem { MediaItemId = 2, ChangeType = FileChangeType.Renamed, OldPath = pathB, NewPath = pathC }
		};
		this._tracker.UnprocessedChanges.AddRange(changes);

		await this._sut.ApplyChangesAsync(changes, false);

		this._tracker.UnprocessedChanges.ShouldBeEmpty();
		await using (var db = new MediaDeckDbContext(this._options)) {
			(await db.MediaItems.FindAsync(1L))!.FilePath.ShouldBe(pathB);
			(await db.MediaItems.FindAsync(2L))!.FilePath.ShouldBe(pathC);
		}
	}

	[Fact]
	public async Task ApplyChangesAsync_OverwriteMove_DeletedAndRenamedTogether_Succeeds() {
		var srcPath = Path.Combine(this._tempRoot, "OverSrc.jpg");
		var dstPath = Path.Combine(this._tempRoot, "OverDst.jpg");

		await using (var db = new MediaDeckDbContext(this._options)) {
			db.MediaItems.AddRange(
				new MediaItem {
					MediaItemId = 1,
					MediaType = MediaType.Image,
					FilePath = srcPath,
					DirectoryPath = this._tempRoot,
					Description = string.Empty,
					IsUnderFolderGroup = false,
				},
				new MediaItem {
					MediaItemId = 2,
					MediaType = MediaType.Image,
					FilePath = dstPath,
					DirectoryPath = this._tempRoot,
					Description = string.Empty,
					IsUnderFolderGroup = false,
				});
			await db.SaveChangesAsync();
		}

		var changes = new[] {
			new FileChangeItem { MediaItemId = 1, ChangeType = FileChangeType.Renamed, OldPath = srcPath, NewPath = dstPath },
			new FileChangeItem { MediaItemId = 2, ChangeType = FileChangeType.Deleted, OldPath = dstPath }
		};
		this._tracker.UnprocessedChanges.AddRange(changes);

		await this._sut.ApplyChangesAsync(changes, false);

		this._tracker.UnprocessedChanges.ShouldBeEmpty();
		await using (var db = new MediaDeckDbContext(this._options)) {
			(await db.MediaItems.FindAsync(1L))!.FilePath.ShouldBe(dstPath);
			(await db.MediaItems.FindAsync(2L)).ShouldBeNull();
		}
	}

	[Fact]
	public async Task ApplyChangesAsync_OverwriteMove_RenamedOnlyWithoutDelete_RollsBackAndNotifiesError() {
		var srcPath = Path.Combine(this._tempRoot, "ConflictSrc.jpg");
		var dstPath = Path.Combine(this._tempRoot, "ConflictDst.jpg");

		await using (var db = new MediaDeckDbContext(this._options)) {
			db.MediaItems.AddRange(
				new MediaItem {
					MediaItemId = 1,
					MediaType = MediaType.Image,
					FilePath = srcPath,
					DirectoryPath = this._tempRoot,
					Description = string.Empty,
					IsUnderFolderGroup = false,
				},
				new MediaItem {
					MediaItemId = 2,
					MediaType = MediaType.Image,
					FilePath = dstPath,
					DirectoryPath = this._tempRoot,
					Description = string.Empty,
					IsUnderFolderGroup = false,
				});
			await db.SaveChangesAsync();
		}

		var notifications = new List<AppNotification>();
		using var sub = this._appNotifDispatcher.Notify.Subscribe(notifications.Add);

		var renamedOnly = new FileChangeItem { MediaItemId = 1, ChangeType = FileChangeType.Renamed, OldPath = srcPath, NewPath = dstPath };
		this._tracker.UnprocessedChanges.Add(renamedOnly);

		await this._sut.ApplyChangesAsync([renamedOnly], false);

		notifications.ShouldContain(n => n.Severity == NotificationSeverity.Error && n.Title == "FileChangeMonitor_ApplyError_Title");
		this._tracker.UnprocessedChanges.ShouldContain(renamedOnly);
		await using (var db = new MediaDeckDbContext(this._options)) {
			(await db.MediaItems.FindAsync(1L))!.FilePath.ShouldBe(srcPath);
		}
	}

	[Fact]
	public async Task ApplyChangesAsync_RechangedDuringApply_DoesNotLoseSubsequentMoveToC() {
		var pathA = Path.Combine(this._tempRoot, "ApplyA.jpg");
		var pathB = Path.Combine(this._tempRoot, "ApplyB.jpg");
		var pathC = Path.Combine(this._tempRoot, "ApplyC.jpg");
		File.WriteAllText(pathB, "apply-content");

		await using (var db = new MediaDeckDbContext(this._options)) {
			db.MediaItems.Add(new MediaItem {
				MediaItemId = 1,
				MediaType = MediaType.Image,
				FilePath = pathA,
				DirectoryPath = this._tempRoot,
				Description = string.Empty,
				IsUnderFolderGroup = false,
			});
			await db.SaveChangesAsync();
		}

		var pausedBeforeDbCommit = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var resumeDbCommit = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var delayedCoord = new Mock<IDatabaseWriteCoordinator>();
		delayedCoord.Setup(x => x.ExecuteAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
			.Returns<Func<CancellationToken, Task>, CancellationToken>(async (op, ct) => {
				pausedBeforeDbCommit.TrySetResult();
				await resumeDbCommit.Task;
				await op(ct);
			});

		using var service = new FileChangeMonitorService(
			this._dbFactoryMock.Object,
			this._tracker,
			new Mock<ILogger<FileChangeMonitorService>>().Object,
			this._appNotifDispatcher,
			this._fileRegistrar,
			delayedCoord.Object,
			this._stringProvider);

		this._tracker.OnRenamed(pathA, pathB);
		await this._tracker.ProcessPendingChangesAsync();
		var itemAtoB = this._tracker.UnprocessedChanges.Single();

		// A -> B の反映を開始し、DB更新の前に一時停止させる
		var applyTask = service.ApplyChangesAsync([itemAtoB], false);
		await pausedBeforeDbCommit.Task.WaitAsync(TimeSpan.FromSeconds(5));

		// 反映中に実体が B -> C へ変更され、Reconcile が UnprocessedChanges を A -> C へ更新
		File.Move(pathB, pathC);
		this._tracker.OnRenamed(pathB, pathC);
		await this._tracker.ProcessPendingChangesAsync();
		this._tracker.UnprocessedChanges.Single().NewPath.ShouldBe(pathC);

		// A -> B の反映を完了させても、C への変更は消えずに B -> C として残る
		resumeDbCommit.SetResult();
		await applyTask;

		this._tracker.UnprocessedChanges.Count.ShouldBe(1);
		var remaining = this._tracker.UnprocessedChanges[0];
		remaining.MediaItemId.ShouldBe(1L);
		remaining.ChangeType.ShouldBe(FileChangeType.Renamed);
		remaining.OldPath.ShouldBe(pathB);
		remaining.NewPath.ShouldBe(pathC);

		await using (var db = new MediaDeckDbContext(this._options)) {
			(await db.MediaItems.FindAsync(1L))!.FilePath.ShouldBe(pathB);
		}
	}

	/// <summary>
	/// 複数の新規ファイルと複数フォルダグループを一括適用した場合に、
	/// 取りこぼしなく全ファイルがフォルダグループより先へキュー投入されることを検証します。
	/// </summary>
	[Fact]
	public async Task ApplyChangesAsync_MultipleAddedFilesAndFolderGroups_EnqueuesAllViaBatchInExpectedOrder() {
		var dir1 = Path.Combine(this._tempRoot, "BatchAlbum1");
		var dir2 = Path.Combine(this._tempRoot, "BatchAlbum2");
		Directory.CreateDirectory(dir1);
		Directory.CreateDirectory(dir2);

		var files = Enumerable.Range(1, 6)
			.Select(i => {
				var p = Path.Combine(i <= 3 ? dir1 : dir2, $"img_{i:D2}.jpg");
				File.WriteAllText(p, $"content-{i}");
				return p;
			})
			.ToList();

		var items = new List<FileChangeItem> {
			new() { ChangeType = FileChangeType.Added, NewPath = dir1 },
			new() { ChangeType = FileChangeType.Added, NewPath = dir2 },
		};
		items.AddRange(files.Select(p => new FileChangeItem { ChangeType = FileChangeType.Added, NewPath = p }));
		this._tracker.UnprocessedChanges.AddRange(items);

		await this._sut.ApplyChangesAsync(items, false);
		await this.WaitForRegisteredCountAsync(files.Count + 2);

		lock (this._registeredPaths) {
			this._registeredPaths.Count.ShouldBe(files.Count + 2);
			var firstDirIndex = Math.Min(this._registeredPaths.IndexOf(dir1), this._registeredPaths.IndexOf(dir2));
			firstDirIndex.ShouldBe(files.Count);
			foreach (var f in files) {
				this._registeredPaths.IndexOf(f).ShouldBeLessThan(firstDirIndex);
			}
		}
	}

	/// <summary>
	/// A -&gt; B の反映処理中に実体が B -&gt; A へ戻って Reconcile で相殺消去された場合でも、
	/// 反映完了後に自動再評価が走り、最新 DB (B) と実体 (A) の差分（B -&gt; A）が再検出されることを検証します。
	/// </summary>
	[Fact]
	public async Task ApplyChangesAsync_RoundTripBackToADuringApply_ReschedulesAndDetectsBToA() {
		var pathA = Path.Combine(this._tempRoot, "RoundApplyA.jpg");
		var pathB = Path.Combine(this._tempRoot, "RoundApplyB.jpg");
		File.WriteAllText(pathB, "roundtrip-apply-content");
		var contentHash = FileHashUtility.ComputeFileHash(pathB);
		var contentSize = new FileInfo(pathB).Length;

		await using (var db = new MediaDeckDbContext(this._options)) {
			db.MediaItems.Add(new MediaItem {
				MediaItemId = 1,
				MediaType = MediaType.Image,
				FilePath = pathA,
				DirectoryPath = this._tempRoot,
				PreHash = contentHash,
				FileSize = contentSize,
				Description = string.Empty,
				IsUnderFolderGroup = false,
			});
			await db.SaveChangesAsync();
		}

		var pausedBeforeDbCommit = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var resumeDbCommit = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var delayedCoord = new Mock<IDatabaseWriteCoordinator>();
		delayedCoord.Setup(x => x.ExecuteAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
			.Returns<Func<CancellationToken, Task>, CancellationToken>(async (op, ct) => {
				pausedBeforeDbCommit.TrySetResult();
				await resumeDbCommit.Task;
				await op(ct);
			});

		this._tracker.StabilityDelay = TimeSpan.FromMilliseconds(50);
		using var service = new FileChangeMonitorService(
			this._dbFactoryMock.Object,
			this._tracker,
			new Mock<ILogger<FileChangeMonitorService>>().Object,
			this._appNotifDispatcher,
			this._fileRegistrar,
			delayedCoord.Object,
			this._stringProvider);

		this._tracker.OnRenamed(pathA, pathB);
		await this._tracker.ProcessPendingChangesAsync();
		var itemAtoB = this._tracker.UnprocessedChanges.Single();

		// A -> B の反映を開始し、DB更新の前に一時停止させる
		var applyTask = service.ApplyChangesAsync([itemAtoB], false);
		await pausedBeforeDbCommit.Task.WaitAsync(TimeSpan.FromSeconds(5));

		// 反映完了前に実体が B -> A へ戻り、Reconcile で UnprocessedChanges が空へ相殺される
		File.Move(pathB, pathA);
		this._tracker.OnRenamed(pathB, pathA);
		await this._tracker.ProcessPendingChangesAsync();
		this._tracker.UnprocessedChanges.ShouldBeEmpty();

		// 遅延していた A -> B の DB 更新が完了すると、自動で再評価がスケジュールされ B -> A が再検出される
		resumeDbCommit.SetResult();
		await applyTask;

		var deadline = DateTime.UtcNow.AddSeconds(5);
		while (this._tracker.UnprocessedChanges.Count == 0 && DateTime.UtcNow < deadline) {
			await Task.Delay(25);
		}

		this._tracker.UnprocessedChanges.Count.ShouldBe(1);
		var redetected = this._tracker.UnprocessedChanges[0];
		redetected.MediaItemId.ShouldBe(1L);
		redetected.ChangeType.ShouldBe(FileChangeType.Moved);
		redetected.OldPath.ShouldBe(pathB);
		redetected.NewPath.ShouldBe(pathA);
	}

	/// <summary>
	/// 上書き移動の連鎖（B 削除 -&gt; A -&gt; B -&gt; C）を検知してそのまま DB へ一括反映しても、
	/// UNIQUE 制約エラーなく正しく更新・削除されることを検証します。
	/// </summary>
	[Fact]
	public async Task ApplyChangesAsync_OverwriteMoveChain_AppliesCleanlyWithoutUniqueConstraintError() {
		var pathA = Path.Combine(this._tempRoot, "ApplyChainOverA.jpg");
		var pathB = Path.Combine(this._tempRoot, "ApplyChainOverB.jpg");
		var pathC = Path.Combine(this._tempRoot, "ApplyChainOverC.jpg");
		File.WriteAllText(pathC, "chain-a-bytes");

		await using (var db = new MediaDeckDbContext(this._options)) {
			db.MediaItems.AddRange(
				new MediaItem {
					MediaItemId = 1,
					MediaType = MediaType.Image,
					FilePath = pathA,
					DirectoryPath = this._tempRoot,
					PreHash = FileHashUtility.ComputeFileHash(pathC),
					FileSize = new FileInfo(pathC).Length,
					Description = string.Empty,
					IsUnderFolderGroup = false,
				},
				new MediaItem {
					MediaItemId = 2,
					MediaType = MediaType.Image,
					FilePath = pathB,
					DirectoryPath = this._tempRoot,
					PreHash = "chain-b-hash",
					FileSize = 123,
					Description = string.Empty,
					IsUnderFolderGroup = false,
				});
			await db.SaveChangesAsync();
		}

		this._tracker.OnDeleted(pathB);
		this._tracker.OnRenamed(pathA, pathB);
		this._tracker.OnRenamed(pathB, pathC);
		await this._tracker.ProcessPendingChangesAsync();

		this._tracker.UnprocessedChanges.Count.ShouldBe(2);
		await this._sut.ApplyChangesAsync(this._tracker.UnprocessedChanges.ToArray(), false);

		this._tracker.UnprocessedChanges.ShouldBeEmpty();
		await using (var db = new MediaDeckDbContext(this._options)) {
			(await db.MediaItems.FindAsync(1L))!.FilePath.ShouldBe(pathC);
			(await db.MediaItems.FindAsync(2L)).ShouldBeNull();
		}
	}

	[Fact]
	public void Dispose_DoesNotThrow() {
		Should.NotThrow(() => this._sut.Dispose());
	}
}