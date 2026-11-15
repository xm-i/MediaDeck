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

	/// <summary>
	/// GetOrCreateAsync で新規アルバムを作成したとき RootAlbum が更新され、他の AlbumRepository インスタンスにも通知されることを確認します。
	/// </summary>
	[Fact]
	public async Task GetOrCreateAsync_NewAlbum_UpdatesRootAlbumAndNotifiesOthers() {
		var dbFactory = CreateInMemoryDbFactory(nameof(GetOrCreateAsync_NewAlbum_UpdatesRootAlbumAndNotifiesOthers));
		using var sut1 = CreateSut(dbFactory);
		using var sut2 = CreateSut(dbFactory);

		await sut1.Load();
		await sut2.Load();
		sut1.RootAlbum.Value.ChildAlbums.ShouldBeEmpty();
		sut2.RootAlbum.Value.ChildAlbums.ShouldBeEmpty();

		await sut1.GetOrCreateAsync("TestAlbum");

		sut1.RootAlbum.Value.ChildAlbums.Length.ShouldBe(1);
		sut1.RootAlbum.Value.ChildAlbums[0].AlbumName.ShouldBe("TestAlbum");

		for (var i = 0; i < 100 && sut2.RootAlbum.Value.ChildAlbums.Length == 0; i++) {
			await Task.Delay(20);
		}
		sut2.RootAlbum.Value.ChildAlbums.Length.ShouldBe(1);
		sut2.RootAlbum.Value.ChildAlbums[0].AlbumName.ShouldBe("TestAlbum");
	}

	/// <summary>
	/// DeleteAlbumAsync で単アルバムを削除するとアルバムと紐付けのみが削除され、メディア本体は残り、他の AlbumRepository にも通知されることを確認します。
	/// </summary>
	[Fact]
	public async Task DeleteAlbumAsync_LeafAlbum_DeletesAlbumAndLinksAndNotifiesOthers() {
		var dbFactory = CreateInMemoryDbFactory(nameof(DeleteAlbumAsync_LeafAlbum_DeletesAlbumAndLinksAndNotifiesOthers));

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

		using var sut1 = CreateSut(dbFactory);
		using var sut2 = CreateSut(dbFactory);
		await sut1.AddItemsAsync("TestAlbum", [1]);
		await sut2.Load();
		sut2.RootAlbum.Value.ChildAlbums.Length.ShouldBe(1);

		var deleted = await sut1.DeleteAlbumAsync("TestAlbum");

		deleted.ShouldBeTrue();
		sut1.RootAlbum.Value.ChildAlbums.ShouldBeEmpty();

		await using (var db = await dbFactory.CreateDbContextAsync()) {
			(await db.Albums.CountAsync()).ShouldBe(0);
			(await db.MediaItemAlbums.CountAsync()).ShouldBe(0);
			(await db.MediaItems.CountAsync()).ShouldBe(1);
		}

		for (var i = 0; i < 100 && sut2.RootAlbum.Value.ChildAlbums.Length > 0; i++) {
			await Task.Delay(20);
		}
		sut2.RootAlbum.Value.ChildAlbums.ShouldBeEmpty();
	}

	/// <summary>
	/// 子アルバムを持つ親ノードは DeleteAlbumAsync で削除できず、子アルバムを削除した後にのみ削除できることを確認します。
	/// </summary>
	[Fact]
	public async Task DeleteAlbumAsync_ParentAlbum_ReturnsFalseAndDoesNotDelete() {
		var dbFactory = CreateInMemoryDbFactory(nameof(DeleteAlbumAsync_ParentAlbum_ReturnsFalseAndDoesNotDelete));
		using var sut = CreateSut(dbFactory);

		await sut.GetOrCreateAsync("Parent");
		await sut.GetOrCreateAsync("Parent/Child");

		sut.RootAlbum.Value.ChildAlbums.Length.ShouldBe(1);
		var parentNode = sut.RootAlbum.Value.ChildAlbums[0];
		parentNode.CanDelete.ShouldBeFalse();
		parentNode.ChildAlbums.Length.ShouldBe(1);
		parentNode.ChildAlbums[0].CanDelete.ShouldBeTrue();

		var deletedParent = await sut.DeleteAlbumAsync("Parent");
		deletedParent.ShouldBeFalse();

		await using (var db = await dbFactory.CreateDbContextAsync()) {
			(await db.Albums.CountAsync()).ShouldBe(2);
		}

		var deletedChild = await sut.DeleteAlbumAsync("Parent/Child");
		deletedChild.ShouldBeTrue();
		sut.RootAlbum.Value.ChildAlbums.Length.ShouldBe(1);
		sut.RootAlbum.Value.ChildAlbums[0].CanDelete.ShouldBeTrue();

		var deletedParentAfterChildRemoved = await sut.DeleteAlbumAsync("Parent");
		deletedParentAfterChildRemoved.ShouldBeTrue();
		sut.RootAlbum.Value.ChildAlbums.ShouldBeEmpty();
	}

	/// <summary>
	/// RenameAlbumAsync で単アルバムをリネームすると DB とツリーが更新され、メディアとの紐付けが維持され、他インスタンスにも通知されることを確認します。
	/// </summary>
	[Fact]
	public async Task RenameAlbumAsync_SingleAlbum_RenamesAndNotifiesOthers() {
		var dbFactory = CreateInMemoryDbFactory(nameof(RenameAlbumAsync_SingleAlbum_RenamesAndNotifiesOthers));

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

		using var sut1 = CreateSut(dbFactory);
		using var sut2 = CreateSut(dbFactory);
		await sut1.AddItemsAsync("OldAlbum", [1]);
		await sut2.Load();

		var result = await sut1.RenameAlbumAsync("OldAlbum", "NewAlbum");

		result.ShouldBe(AlbumRenameResult.Success);
		sut1.RootAlbum.Value.ChildAlbums.Length.ShouldBe(1);
		sut1.RootAlbum.Value.ChildAlbums[0].AlbumPath.ShouldBe("NewAlbum");
		sut1.RootAlbum.Value.ChildAlbums[0].FileCount.ShouldBe(1);

		await using (var db = await dbFactory.CreateDbContextAsync()) {
			var albums = await db.Albums.Include(a => a.MediaItemAlbums).ToListAsync();
			albums.Count.ShouldBe(1);
			albums[0].Path.ShouldBe("NewAlbum");
			albums[0].MediaItemAlbums.Count.ShouldBe(1);
		}

		for (var i = 0; i < 100 && sut2.RootAlbum.Value.ChildAlbums[0].AlbumPath != "NewAlbum"; i++) {
			await Task.Delay(20);
		}
		sut2.RootAlbum.Value.ChildAlbums[0].AlbumPath.ShouldBe("NewAlbum");
	}

	/// <summary>
	/// RenameAlbumAsync で親ノードをリネームすると配下の全サブアルバムのプレフィックスも一括更新されることを確認します。
	/// </summary>
	[Fact]
	public async Task RenameAlbumAsync_ParentAndChildAlbums_RenamesPrefix() {
		var dbFactory = CreateInMemoryDbFactory(nameof(RenameAlbumAsync_ParentAndChildAlbums_RenamesPrefix));
		using var sut = CreateSut(dbFactory);

		await sut.GetOrCreateAsync("Travel/2024");
		await sut.GetOrCreateAsync("Travel/2025/Summer");

		var result = await sut.RenameAlbumAsync("Travel", "Trip");

		result.ShouldBe(AlbumRenameResult.Success);
		sut.RootAlbum.Value.ChildAlbums.Length.ShouldBe(1);
		sut.RootAlbum.Value.ChildAlbums[0].AlbumName.ShouldBe("Trip");

		await using (var db = await dbFactory.CreateDbContextAsync()) {
			var paths = await db.Albums.Select(a => a.Path).OrderBy(p => p).ToListAsync();
			paths.ShouldBe(["Trip/2024", "Trip/2025/Summer"]);
		}
	}

	/// <summary>
	/// RenameAlbumAsync で既存パスまたは既存親グループと重複する場合は DuplicatePath を返し DB を変更しないことを確認します。
	/// </summary>
	[Fact]
	public async Task RenameAlbumAsync_DuplicatePath_ReturnsDuplicatePath() {
		var dbFactory = CreateInMemoryDbFactory(nameof(RenameAlbumAsync_DuplicatePath_ReturnsDuplicatePath));
		using var sut = CreateSut(dbFactory);

		await sut.GetOrCreateAsync("AlbumA");
		await sut.GetOrCreateAsync("AlbumB");
		await sut.GetOrCreateAsync("Group/One");
		await sut.GetOrCreateAsync("Group/Two");

		(await sut.RenameAlbumAsync("AlbumA", "albumB")).ShouldBe(AlbumRenameResult.DuplicatePath);
		(await sut.RenameAlbumAsync("AlbumA", "Group")).ShouldBe(AlbumRenameResult.DuplicatePath);
		(await sut.RenameAlbumAsync("AlbumA", "Group/Three")).ShouldBe(AlbumRenameResult.Success);
	}

	/// <summary>
	/// RenameAlbumAsync で変更元自身の配下へ移動しようとした場合は InvalidDestinationUnderSource を返すことを確認します。
	/// </summary>
	[Fact]
	public async Task RenameAlbumAsync_DestinationUnderSource_ReturnsInvalidDestinationUnderSource() {
		var dbFactory = CreateInMemoryDbFactory(nameof(RenameAlbumAsync_DestinationUnderSource_ReturnsInvalidDestinationUnderSource));
		using var sut = CreateSut(dbFactory);

		await sut.GetOrCreateAsync("Travel/2024");
		await sut.GetOrCreateAsync("Travel/2025");

		var result = await sut.RenameAlbumAsync("Travel", "Travel/Sub");

		result.ShouldBe(AlbumRenameResult.InvalidDestinationUnderSource);
	}

	/// <summary>
	/// RenameAlbumAsync で大文字・小文字のみの変更が正常に反映されることを確認します。
	/// </summary>
	[Fact]
	public async Task RenameAlbumAsync_CaseOnlyChange_UpdatesCasing() {
		var dbFactory = CreateInMemoryDbFactory(nameof(RenameAlbumAsync_CaseOnlyChange_UpdatesCasing));
		using var sut = CreateSut(dbFactory);

		await sut.GetOrCreateAsync("travel");
		await sut.GetOrCreateAsync("travel/2024");

		var result = await sut.RenameAlbumAsync("travel", "Travel");

		result.ShouldBe(AlbumRenameResult.Success);
		sut.RootAlbum.Value.ChildAlbums[0].AlbumPath.ShouldBe("Travel");
		sut.RootAlbum.Value.ChildAlbums[0].ChildAlbums[0].AlbumPath.ShouldBe("Travel/2024");
	}
}