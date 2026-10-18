using MediaDeck.Composition.Database;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.Services;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Composition.Stores.Config.Model.Objects;
using MediaDeck.Composition.Stores.State.Model.Objects;
using MediaDeck.Composition.Tables;
using MediaDeck.Core.Models.Files;
using MediaDeck.Core.Models.NotificationDispatcher;
using MediaDeck.Core.Services.FileChangeMonitor;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Moq;

using Shouldly;

namespace MediaDeck.Core.Tests.Services.FileChangeMonitor;

public class FileChangeMonitorServiceTests {
	private readonly FileChangeMonitorService _sut;
	private readonly FileChangeTracker _tracker;
	private readonly DbContextOptions<MediaDeckDbContext> _options;

	public FileChangeMonitorServiceTests() {
		this._options = new DbContextOptionsBuilder<MediaDeckDbContext>()
			.UseInMemoryDatabase($"FCM_{Guid.NewGuid()}")
			.Options;
		var dbFactoryMock = new Mock<IDbContextFactory<MediaDeckDbContext>>();
		dbFactoryMock.Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(() => new MediaDeckDbContext(this._options));

		var trackerLogger = new Mock<ILogger<FileChangeTracker>>();
		this._tracker = new FileChangeTracker(dbFactoryMock.Object, trackerLogger.Object);

		var logger = new Mock<ILogger<FileChangeMonitorService>>();
		var appNotifDispatcher = new AppNotificationDispatcher();
		var dbWriteCoord = new Mock<IDatabaseWriteCoordinator>();
		dbWriteCoord.Setup(x => x.ExecuteAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
			.Returns<Func<CancellationToken, Task>, CancellationToken>(async (op, ct) => await op(ct));

		// Build ConfigModel via DI
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
		services.AddSingleton<MediaDeck.Composition.Interfaces.IStringProvider, StubStringProvider>();
		services.AddSingleton<ConfigModel>();
		services.AddTransient<SortObject>(sp => new SortObject(sp));
		services.AddTransient<SortItemObject>();
		var provider = services.BuildServiceProvider();
		var config = provider.GetRequiredService<ConfigModel>();

		var mediaItemTypeService = new Mock<MediaDeck.Composition.Interfaces.MediaItemTypes.IMediaItemTypeService>();
		mediaItemTypeService.Setup(x => x.CreateMediaItemOperators()).Returns([]);
		var fileRegistrar = new FileRegistrar(config, new Mock<ILogger<FileRegistrar>>().Object, mediaItemTypeService.Object);

		this._sut = new FileChangeMonitorService(dbFactoryMock.Object, this._tracker, logger.Object, appNotifDispatcher, fileRegistrar, dbWriteCoord.Object);
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

		this._tracker.UnprocessedChanges.ShouldNotContain(item);
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
	public void Dispose_DoesNotThrow() {
		Should.NotThrow(() => this._sut.Dispose());
	}
}