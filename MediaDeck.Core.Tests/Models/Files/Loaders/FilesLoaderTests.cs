using MediaDeck.Composition.Database;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;
using MediaDeck.Composition.Interfaces.Notifications;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Composition.Stores.State.Model.Objects;
using MediaDeck.Composition.Tables;
using MediaDeck.Core.Models.Files.Filter;
using MediaDeck.Core.Models.Files.Loaders;
using MediaDeck.Core.Models.Files.Sort;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using R3;
using Shouldly;

namespace MediaDeck.Core.Tests.Models.Files.Loaders;

public class FilesLoaderTests {
	private static IDbContextFactory<MediaDeckDbContext> CreateInMemoryDbFactory(string dbName) {
		var options = new DbContextOptionsBuilder<MediaDeckDbContext>()
			.UseInMemoryDatabase(databaseName: dbName)
			.ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
			.Options;

		var factoryMock = new Mock<IDbContextFactory<MediaDeckDbContext>>();
		factoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(() => new MediaDeckDbContext(options));

		return factoryMock.Object;
	}

	private static (FilesLoader loader, IDbContextFactory<MediaDeckDbContext> dbFactory) CreateSut(string testName) {
		var dbFactory = CreateInMemoryDbFactory(testName);

		// SortSelector
		var sortDispatcherMock = new Mock<ISearchConditionNotificationDispatcher>();
		sortDispatcherMock.Setup(x => x.SortChanged).Returns(new Subject<Unit>());
		var spMock = new Mock<IServiceProvider>();
		spMock.Setup(x => x.GetService(typeof(SortObject))).Returns(() => new SortObject(spMock.Object));
		spMock.Setup(x => x.GetService(typeof(SortItemObject))).Returns(() => new SortItemObject());
		var searchDefs = new SearchDefinitionsConfigModel(spMock.Object, new StubStringProvider());
		var services = new ServiceCollection();
		MediaDeck.Composition.DIRegistration.AddGeneratedServices(services);
		var tabState = services.BuildServiceProvider().GetRequiredService<TabStateModel>();
		var sortSelector = new SortSelector(tabState, searchDefs, sortDispatcherMock.Object);

		// FilterSelector
		var filterDispatcherMock = new Mock<ISearchConditionNotificationDispatcher>();
		filterDispatcherMock.Setup(x => x.FilterChanged).Returns(new Subject<Unit>());
		var filterSelector = new FilterSelector(tabState, searchDefs, filterDispatcherMock.Object);

		// MediaItemTypeService
		var mediaItemTypeMock = new Mock<IMediaItemTypeService>();
		mediaItemTypeMock.Setup(x => x.CreateMediaItemModelFromRecord(It.IsAny<MediaItem>()))
			.Returns((MediaItem item) => {
				var modelMock = new Mock<IMediaItemModel>();
				modelMock.Setup(m => m.Id).Returns(item.MediaItemId);
				return modelMock.Object;
			});
		mediaItemTypeMock.Setup(x => x.IncludeTables(It.IsAny<IQueryable<MediaItem>>()))
			.Returns((IQueryable<MediaItem> q) => q);

		var loader = new FilesLoader(dbFactory, sortSelector, filterSelector, mediaItemTypeMock.Object);
		return (loader, dbFactory);
	}

	private static async Task SeedItems(IDbContextFactory<MediaDeckDbContext> dbFactory, int count) {
		await using var db = await dbFactory.CreateDbContextAsync();
		for (var i = 1; i <= count; i++) {
			db.MediaItems.Add(new MediaItem {
				MediaItemId = i,
				FilePath = $"/files/file{i}.jpg",
				DirectoryPath = "/files",
				MediaType = MediaType.Image,
				Description = "",
				IsUnderFolderGroup = false,
				ThumbnailFileName = "",
				FileSize = 100 * i,
			});
		}
		await db.SaveChangesAsync();
	}

	[Fact]
	public async Task GetTotalCountAsync_EmptyDb_ReturnsZero() {
		var (loader, _) = CreateSut(nameof(GetTotalCountAsync_EmptyDb_ReturnsZero));
		var count = await loader.GetTotalCountAsync([]);
		count.ShouldBe(0);
	}

	[Fact]
	public async Task GetTotalCountAsync_WithItems_ReturnsCount() {
		var (loader, dbFactory) = CreateSut(nameof(GetTotalCountAsync_WithItems_ReturnsCount));
		await SeedItems(dbFactory, 5);

		var count = await loader.GetTotalCountAsync([]);
		count.ShouldBe(5);
	}

	[Fact]
	public async Task GetFilesStreamAsync_ReturnsAllItems() {
		var (loader, dbFactory) = CreateSut(nameof(GetFilesStreamAsync_ReturnsAllItems));
		await SeedItems(dbFactory, 3);

		var items = new List<IMediaItemModel>();
		await foreach (var item in loader.GetFilesStreamAsync([])) {
			items.Add(item);
		}
		items.Count.ShouldBe(3);
	}

	[Fact]
	public async Task GetFilesStreamAsync_WithTake_LimitsResults() {
		var (loader, dbFactory) = CreateSut(nameof(GetFilesStreamAsync_WithTake_LimitsResults));
		await SeedItems(dbFactory, 5);

		var items = new List<IMediaItemModel>();
		await foreach (var item in loader.GetFilesStreamAsync([], take: 2)) {
			items.Add(item);
		}
		items.Count.ShouldBe(2);
	}

	[Fact]
	public async Task GetFilesStreamAsync_WithSkip_SkipsItems() {
		var (loader, dbFactory) = CreateSut(nameof(GetFilesStreamAsync_WithSkip_SkipsItems));
		await SeedItems(dbFactory, 5);

		var items = new List<IMediaItemModel>();
		await foreach (var item in loader.GetFilesStreamAsync([], skip: 3)) {
			items.Add(item);
		}
		items.Count.ShouldBe(2);
	}
	private static (FilesLoader loader, IDbContextFactory<MediaDeckDbContext> dbFactory) CreateRandomSut(string testName) {
		var dbFactory = CreateInMemoryDbFactory(testName);

		var sortDispatcherMock = new Mock<ISearchConditionNotificationDispatcher>();
		sortDispatcherMock.Setup(x => x.SortChanged).Returns(new Subject<Unit>());
		var spMock = new Mock<IServiceProvider>();
		spMock.Setup(x => x.GetService(typeof(SortObject))).Returns(() => new SortObject(spMock.Object));
		spMock.Setup(x => x.GetService(typeof(SortItemObject))).Returns(() => new SortItemObject());
		var searchDefs = new SearchDefinitionsConfigModel(spMock.Object, new StubStringProvider());
		var services = new ServiceCollection();
		MediaDeck.Composition.DIRegistration.AddGeneratedServices(services);
		var tabState = services.BuildServiceProvider().GetRequiredService<TabStateModel>();

		var randomSort = searchDefs.AddSortCondition();
		var sortItem = randomSort.AddSortItemObject();
		sortItem.SortItemKey = SortItemKey.Random;
		tabState.SearchState.CurrentSortCondition.Value = randomSort.Id;

		var sortSelector = new SortSelector(tabState, searchDefs, sortDispatcherMock.Object);

		var filterDispatcherMock = new Mock<ISearchConditionNotificationDispatcher>();
		filterDispatcherMock.Setup(x => x.FilterChanged).Returns(new Subject<Unit>());
		var filterSelector = new FilterSelector(tabState, searchDefs, filterDispatcherMock.Object);

		var mediaItemTypeMock = new Mock<IMediaItemTypeService>();
		mediaItemTypeMock.Setup(x => x.CreateMediaItemModelFromRecord(It.IsAny<MediaItem>()))
			.Returns((MediaItem item) => {
				var modelMock = new Mock<IMediaItemModel>();
				modelMock.Setup(m => m.Id).Returns(item.MediaItemId);
				return modelMock.Object;
			});
		mediaItemTypeMock.Setup(x => x.IncludeTables(It.IsAny<IQueryable<MediaItem>>()))
			.Returns((IQueryable<MediaItem> q) => q);

		var loader = new FilesLoader(dbFactory, sortSelector, filterSelector, mediaItemTypeMock.Object);
		return (loader, dbFactory);
	}

	[Fact]
	public async Task GetFilesStreamAsync_RandomSort_ReturnsAllItemsWithoutDuplication() {
		var (loader, dbFactory) = CreateRandomSut(nameof(GetFilesStreamAsync_RandomSort_ReturnsAllItemsWithoutDuplication));
		await SeedItems(dbFactory, 10);

		var items = new List<IMediaItemModel>();
		await foreach (var item in loader.GetFilesStreamAsync([])) {
			items.Add(item);
		}
		items.Count.ShouldBe(10);
		items.Select(x => x.Id).Distinct().Count().ShouldBe(10);
	}

	[Fact]
	public async Task GetFilesStreamAsync_RandomSort_PaginationMaintainsConsistencyWithoutDuplicates() {
		var (loader, dbFactory) = CreateRandomSut(nameof(GetFilesStreamAsync_RandomSort_PaginationMaintainsConsistencyWithoutDuplicates));
		await SeedItems(dbFactory, 10);

		var page1 = new List<IMediaItemModel>();
		await foreach (var item in loader.GetFilesStreamAsync([], skip: 0, take: 5)) {
			page1.Add(item);
		}

		var page2 = new List<IMediaItemModel>();
		await foreach (var item in loader.GetFilesStreamAsync([], skip: 5, take: 5)) {
			page2.Add(item);
		}

		page1.Count.ShouldBe(5);
		page2.Count.ShouldBe(5);

		var allIds = page1.Concat(page2).Select(x => x.Id).ToList();
		allIds.Count.ShouldBe(10);
		allIds.Distinct().Count().ShouldBe(10);
	}

	[Fact]
	public async Task GetTotalCountAsync_RandomSort_ReturnsCount() {
		var (loader, dbFactory) = CreateRandomSut(nameof(GetTotalCountAsync_RandomSort_ReturnsCount));
		await SeedItems(dbFactory, 7);

		// 一度ストリームを読んでシャッフルキャッシュを生成
		var firstItem = (await loader.GetFilesStreamAsync([], skip: 0, take: 2).ToListAsync()).Count;
		firstItem.ShouldBe(2);

		var count = await loader.GetTotalCountAsync([]);
		count.ShouldBe(7);
	}
}