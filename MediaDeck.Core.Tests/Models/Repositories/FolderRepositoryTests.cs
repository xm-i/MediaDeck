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

public class FolderRepositoryTests {
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

	private static FolderRepository CreateSut(IDbContextFactory<MediaDeckDbContext> dbFactory) {
		var dispatcherMock = new Mock<ISearchConditionNotificationDispatcher>();
		dispatcherMock.Setup(x => x.UpdateRequest).Returns(new Subject<Action<ObservableList<ISearchCondition>>>());
		var services = new ServiceCollection();
		MediaDeck.Composition.DIRegistration.AddGeneratedServices(services);
		var tabState = services.BuildServiceProvider().GetRequiredService<TabStateModel>();
		return new FolderRepository(dbFactory, dispatcherMock.Object, tabState);
	}

	/// <summary>
	///     Loadで空のDBからルートフォルダが作成されることを確認します。
	/// </summary>
	[Fact]
	public async Task Load_EmptyDb_CreatesRootFolder() {
		var dbFactory = CreateInMemoryDbFactory(nameof(Load_EmptyDb_CreatesRootFolder));
		using var sut = CreateSut(dbFactory);

		await sut.Load();

		sut.RootFolder.Value.ShouldNotBeNull();
		sut.RootFolder.Value.FolderPath.ShouldBe("");
	}

	/// <summary>
	///     Loadでメディアアイテムを含むDBからフォルダツリーが構築されることを確認します。
	/// </summary>
	[Fact]
	public async Task Load_WithMediaItems_BuildsFolderTree() {
		var dbFactory = CreateInMemoryDbFactory(nameof(Load_WithMediaItems_BuildsFolderTree));

		await using (var db = await dbFactory.CreateDbContextAsync()) {
			db.MediaItems.Add(new MediaItem {
				MediaItemId = 1,
				FilePath = $"C:{Path.DirectorySeparatorChar}Photos{Path.DirectorySeparatorChar}img1.jpg",
				DirectoryPath = $"C:{Path.DirectorySeparatorChar}Photos",
				MediaType = MediaType.Image,
				Description = "",
				IsUnderFolderGroup = false,
				ThumbnailFileName = "",
				FileSize = 100,
			});
			db.MediaItems.Add(new MediaItem {
				MediaItemId = 2,
				FilePath = $"C:{Path.DirectorySeparatorChar}Videos{Path.DirectorySeparatorChar}vid1.mp4",
				DirectoryPath = $"C:{Path.DirectorySeparatorChar}Videos",
				MediaType = MediaType.Video,
				Description = "",
				IsUnderFolderGroup = false,
				ThumbnailFileName = "",
				FileSize = 200,
			});
			await db.SaveChangesAsync();
		}

		using var sut = CreateSut(dbFactory);

		await sut.Load();

		sut.RootFolder.Value.ShouldNotBeNull();
		sut.RootFolder.Value.ChildFolders.ShouldNotBeEmpty();
	}

	/// <summary>
	///     同一データでLoadを2回呼ぶとキャッシュされることを確認します。
	/// </summary>
	[Fact]
	public async Task Load_CalledTwiceWithSameData_DoesNotRebuildTree() {
		var dbFactory = CreateInMemoryDbFactory(nameof(Load_CalledTwiceWithSameData_DoesNotRebuildTree));
		using var sut = CreateSut(dbFactory);

		await sut.Load();
		var first = sut.RootFolder.Value;

		await sut.Load();
		var second = sut.RootFolder.Value;

		// 同じデータならオブジェクトは変わらない（SequenceEqualでスキップされる）
		first.ShouldBeSameAs(second);
	}
}