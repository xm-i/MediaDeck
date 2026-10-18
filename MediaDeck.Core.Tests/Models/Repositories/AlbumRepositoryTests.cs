using MediaDeck.Composition.Database;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Composition.Interfaces.Notifications;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Composition.Tables;
using MediaDeck.Core.Models.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ObservableCollections;
using R3;
using Shouldly;

namespace MediaDeck.Core.Tests.Models.Repositories;

public class AlbumRepositoryTests {
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

	private static AlbumRepository CreateSut(IDbContextFactory<MediaDeckDbContext> dbFactory) {
		var dispatcherMock = new Mock<ISearchConditionNotificationDispatcher>();
		dispatcherMock.Setup(x => x.UpdateRequest).Returns(new Subject<Action<ObservableList<ISearchCondition>>>());
		var services = new ServiceCollection();
		MediaDeck.Composition.DIRegistration.AddGeneratedServices(services);
		var tabState = services.BuildServiceProvider().GetRequiredService<TabStateModel>();
		return new AlbumRepository(dbFactory, dispatcherMock.Object, tabState);
	}

	/// <summary>
	///     Loadで空のDBからルートアルバムが作成されることを確認します。
	/// </summary>
	[Fact]
	public async Task Load_EmptyDb_CreatesRootAlbum() {
		var dbFactory = CreateInMemoryDbFactory(nameof(Load_EmptyDb_CreatesRootAlbum));
		using var sut = CreateSut(dbFactory);

		await sut.Load();

		sut.RootAlbum.Value.ShouldNotBeNull();
		sut.RootAlbum.Value.AlbumPath.ShouldBe("");
	}

	/// <summary>
	///     Loadでアルバムを含むDBからツリーが構築されることを確認します。
	/// </summary>
	[Fact]
	public async Task Load_WithAlbums_BuildsTree() {
		var dbFactory = CreateInMemoryDbFactory(nameof(Load_WithAlbums_BuildsTree));

		// Seed
		await using (var db = await dbFactory.CreateDbContextAsync()) {
			db.Albums.Add(new Album { Path = "Travel/2024", MediaItemAlbums = [] });
			db.Albums.Add(new Album { Path = "Travel/2025", MediaItemAlbums = [] });
			await db.SaveChangesAsync();
		}

		using var sut = CreateSut(dbFactory);

		await sut.Load();

		sut.RootAlbum.Value.ShouldNotBeNull();
		sut.RootAlbum.Value.ChildAlbums.ShouldNotBeEmpty();
	}

	/// <summary>
	///     GetOrCreateAsyncが存在しないパスを作成することを確認します。
	/// </summary>
	[Fact]
	public async Task GetOrCreateAsync_CreatesNewAlbum() {
		var dbFactory = CreateInMemoryDbFactory(nameof(GetOrCreateAsync_CreatesNewAlbum));
		using var sut = CreateSut(dbFactory);

		var result = await sut.GetOrCreateAsync("NewAlbum/Sub");

		result.ShouldNotBeNull();
		result.Path.ShouldBe("NewAlbum/Sub");
	}

	/// <summary>
	///     GetOrCreateAsyncが既存のアルバムを返すことを確認します。
	/// </summary>
	[Fact]
	public async Task GetOrCreateAsync_ReturnsExisting() {
		var dbFactory = CreateInMemoryDbFactory(nameof(GetOrCreateAsync_ReturnsExisting));

		await using (var db = await dbFactory.CreateDbContextAsync()) {
			db.Albums.Add(new Album { Path = "Existing", MediaItemAlbums = [] });
			await db.SaveChangesAsync();
		}

		using var sut = CreateSut(dbFactory);
		var result = await sut.GetOrCreateAsync("Existing");

		result.ShouldNotBeNull();
		result.Path.ShouldBe("Existing");
	}

	/// <summary>
	///     AddItemsAsyncがアルバムにアイテムを追加することを確認します。
	/// </summary>
	[Fact]
	public async Task AddItemsAsync_AddsMediaItems() {
		var dbFactory = CreateInMemoryDbFactory(nameof(AddItemsAsync_AddsMediaItems));

		// Seed a media item
		await using (var db = await dbFactory.CreateDbContextAsync()) {
			db.MediaItems.Add(new MediaItem {
				MediaItemId = 1,
				FilePath = "/test.jpg",
				DirectoryPath = "/",
				MediaType = MediaType.Image,
				Description = "",
				IsUnderFolderGroup = false,
				ThumbnailFileName = "",
				FileSize = 100,
			});
			await db.SaveChangesAsync();
		}

		using var sut = CreateSut(dbFactory);
		await sut.AddItemsAsync("TestAlbum", [1]);

		await using (var db = await dbFactory.CreateDbContextAsync()) {
			var links = await db.MediaItemAlbums.ToListAsync();
			links.Count.ShouldBe(1);
			links[0].MediaItemId.ShouldBe(1);
		}
	}

	/// <summary>
	///     GetRecentAlbumsAsyncが最近のアルバムを返すことを確認します。
	/// </summary>
	[Fact]
	public async Task GetRecentAlbumsAsync_ReturnsAlbums() {
		var dbFactory = CreateInMemoryDbFactory(nameof(GetRecentAlbumsAsync_ReturnsAlbums));

		await using (var db = await dbFactory.CreateDbContextAsync()) {
			db.Albums.Add(new Album { Path = "A", MediaItemAlbums = [] });
			db.Albums.Add(new Album { Path = "B", MediaItemAlbums = [] });
			await db.SaveChangesAsync();
		}

		using var sut = CreateSut(dbFactory);
		var result = await sut.GetRecentAlbumsAsync();

		result.Count.ShouldBe(2);
	}

	/// <summary>
	///     空パスでGetOrCreateAsyncを呼ぶと例外がスローされることを確認します。
	/// </summary>
	[Fact]
	public async Task GetOrCreateAsync_EmptyPath_ThrowsArgumentException() {
		var dbFactory = CreateInMemoryDbFactory(nameof(GetOrCreateAsync_EmptyPath_ThrowsArgumentException));
		using var sut = CreateSut(dbFactory);

		await Should.ThrowAsync<ArgumentException>(() => sut.GetOrCreateAsync(""));
	}
}