using MediaDeck.Composition.Database;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Composition.Interfaces.Notifications;
using MediaDeck.Composition.Interfaces.Threading;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Composition.Tables;
using MediaDeck.Core.Models.Files.SearchConditions;
using MediaDeck.Core.Models.Repositories;
using MediaDeck.Core.Models.Repositories.Objects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ObservableCollections;
using R3;
using Shouldly;

namespace MediaDeck.Core.Tests.Models.Repositories;

/// <summary>
/// <see cref="AlbumRepository"/> の単体テスト。
/// </summary>
public sealed class AlbumRepositoryTests : IDisposable {
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

	private static AlbumRepository CreateSut(
		IDbContextFactory<MediaDeckDbContext> dbFactory,
		IUiDispatcher? uiDispatcher = null,
		TabStateModel? tabState = null) {
		var dispatcherMock = new Mock<ISearchConditionNotificationDispatcher>();
		dispatcherMock.Setup(x => x.UpdateRequest).Returns(new Subject<Action<ObservableList<ISearchCondition>>>());
		if (tabState == null) {
			var services = new ServiceCollection();
			MediaDeck.Composition.DIRegistration.AddGeneratedServices(services);
			tabState = services.BuildServiceProvider().GetRequiredService<TabStateModel>();
		}
		return new AlbumRepository(dbFactory, dispatcherMock.Object, tabState, uiDispatcher ?? new RecordingUiDispatcher());
	}

	private static TabStateModel CreateTabState() {
		var services = new ServiceCollection();
		MediaDeck.Composition.DIRegistration.AddGeneratedServices(services);
		return services.BuildServiceProvider().GetRequiredService<TabStateModel>();
	}

	/// <summary>
	///     Loadで空のDBからルートアルバムが作成されることを確認します。
	/// </summary>
	[Fact]
	public async Task Load_EmptyDb_CreatesRootAlbum() {
		var dbFactory = this.CreateSqliteDbFactory();
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
		var dbFactory = this.CreateSqliteDbFactory();

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
		var dbFactory = this.CreateSqliteDbFactory();
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
		var dbFactory = this.CreateSqliteDbFactory();

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
		var dbFactory = this.CreateSqliteDbFactory();

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
		var dbFactory = this.CreateSqliteDbFactory();

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
		var dbFactory = this.CreateSqliteDbFactory();
		using var sut = CreateSut(dbFactory);

		await Should.ThrowAsync<ArgumentException>(() => sut.GetOrCreateAsync(""));
	}

	/// <summary>
	/// GetOrCreateAsync で新規アルバムを作成したとき RootAlbum が更新され、他の AlbumRepository インスタンスにも通知されることを確認します。
	/// </summary>
	[Fact]
	public async Task GetOrCreateAsync_NewAlbum_UpdatesRootAlbumAndNotifiesOthers() {
		var dbFactory = this.CreateSqliteDbFactory();
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
		var dbFactory = this.CreateSqliteDbFactory();

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
		var dbFactory = this.CreateSqliteDbFactory();
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
		var dbFactory = this.CreateSqliteDbFactory();

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
		var dbFactory = this.CreateSqliteDbFactory();
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
		var dbFactory = this.CreateSqliteDbFactory();
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
		var dbFactory = this.CreateSqliteDbFactory();
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
		var dbFactory = this.CreateSqliteDbFactory();
		using var sut = CreateSut(dbFactory);

		await sut.GetOrCreateAsync("travel");
		await sut.GetOrCreateAsync("travel/2024");

		var result = await sut.RenameAlbumAsync("travel", "Travel");

		result.ShouldBe(AlbumRenameResult.Success);
		sut.RootAlbum.Value.ChildAlbums[0].AlbumPath.ShouldBe("Travel");
		sut.RootAlbum.Value.ChildAlbums[0].ChildAlbums[0].AlbumPath.ShouldBe("Travel/2024");
	}

	/// <summary>
	/// Load が呼び出し元スレッドとは別のバックグラウンドスレッドで DB クエリを実行し、IUiDispatcher 経由で RootAlbum を更新することを確認します。
	/// </summary>
	[Fact]
	public void Load_RunsDbQueryOffCallerThread_AndUpdatesRootAlbumViaUiDispatcher() {
		var baseFactory = this.CreateSqliteDbFactory();
		using (var db = baseFactory.CreateDbContext()) {
			db.Albums.Add(new Album { Path = "Travel/2024", MediaItemAlbums = [] });
			db.SaveChanges();
		}

		var trackingFactory = new TrackingDbContextFactory(baseFactory);
		var uiDispatcher = new RecordingUiDispatcher();
		using var sut = CreateSut(trackingFactory, uiDispatcher);

		int? callerThreadId = null;
		Exception? threadException = null;
		var thread = new Thread(() => {
			try {
				callerThreadId = Environment.CurrentManagedThreadId;
				sut.Load().GetAwaiter().GetResult();
			} catch (Exception ex) {
				threadException = ex;
			}
		});
		thread.IsBackground = true;
		thread.Start();
		thread.Join(TimeSpan.FromSeconds(10)).ShouldBeTrue();

		threadException.ShouldBeNull();
		callerThreadId.ShouldNotBeNull();
		trackingFactory.DbQueryThreadIds.ShouldNotBeEmpty();
		trackingFactory.DbQueryThreadIds.ShouldAllBe(id => id != callerThreadId.Value);
		uiDispatcher.RunAsyncActionCount.ShouldBe(1);
		sut.RootAlbum.Value.ShouldNotBeNull();
		sut.RootAlbum.Value.ChildAlbums.Length.ShouldBe(1);
	}

	/// <summary>
	/// AlbumSearchCondition が設定されている場合、RootAlbum 更新通知の時点で IsExpanded が既に復元されていることを確認します。
	/// </summary>
	[Fact]
	public async Task Load_WithAlbumSearchCondition_RestoresIsExpandedBeforePublishingRootAlbum() {
		var dbFactory = this.CreateSqliteDbFactory();
		await using (var db = await dbFactory.CreateDbContextAsync()) {
			db.Albums.Add(new Album { Path = "Travel/2024", MediaItemAlbums = [] });
			db.Albums.Add(new Album { Path = "Travel/2025", MediaItemAlbums = [] });
			await db.SaveChangesAsync();
		}

		var tabState = CreateTabState();
		tabState.SearchState.SearchCondition.Add(new AlbumSearchCondition {
			AlbumPath = "Travel/2024",
			IncludeSubAlbums = true,
		});

		using var sut = CreateSut(dbFactory, tabState: tabState);
		AlbumObject? observedRoot = null;
		bool? observedParentExpanded = null;
		bool? observedTargetExpanded = null;
		using var subscription = sut.RootAlbum
			.Where(root => root != null)
			.Subscribe(root => {
				observedRoot = root;
				var travel = root.ChildAlbums.FirstOrDefault(x => x.AlbumName == "Travel");
				observedParentExpanded = travel?.IsExpanded;
				observedTargetExpanded = travel?.ChildAlbums.FirstOrDefault(x => x.AlbumName == "2024")?.IsExpanded;
			});

		await sut.Load();

		observedRoot.ShouldNotBeNull();
		observedParentExpanded.ShouldBe(true);
		observedTargetExpanded.ShouldBe(true);
	}

	/// <summary>
	/// 同一データで Load を 2 回呼び出してもツリーが再構築されず、UI ディスパッチャーも再呼び出しされないことを確認します。
	/// </summary>
	[Fact]
	public async Task Load_CalledTwiceWithSameData_DoesNotRebuildTreeOrInvokeUiDispatcherAgain() {
		var dbFactory = this.CreateSqliteDbFactory();
		var uiDispatcher = new RecordingUiDispatcher();
		using var sut = CreateSut(dbFactory, uiDispatcher);

		await sut.Load();
		var first = sut.RootAlbum.Value;
		var countAfterFirst = uiDispatcher.RunAsyncActionCount;

		await sut.Load();
		var second = sut.RootAlbum.Value;

		first.ShouldBeSameAs(second);
		uiDispatcher.RunAsyncActionCount.ShouldBe(countAfterFirst);
	}

	/// <summary>
	/// Dispose 後に Load を呼び出しても UI ディスパッチャー呼び出しや RootAlbum 更新が行われないことを確認します。
	/// </summary>
	[Fact]
	public async Task Load_WhenDisposed_SkipsUiUpdate() {
		var dbFactory = this.CreateSqliteDbFactory();
		var uiDispatcher = new RecordingUiDispatcher();
		var sut = CreateSut(dbFactory, uiDispatcher);

		sut.Dispose();
		await sut.Load();

		uiDispatcher.RunAsyncActionCount.ShouldBe(0);
		sut.RootAlbum.Value.ShouldBeNull();
	}

	/// <summary>
	/// 同期的にコールバックを実行しつつ呼び出し回数を記録するテスト用 <see cref="IUiDispatcher"/> 実装。
	/// </summary>
	private sealed class RecordingUiDispatcher : IUiDispatcher {
		private int _runAsyncActionCount;

		/// <summary>
		/// <see cref="RunAsync(Action)"/> が呼ばれた回数を取得します。
		/// </summary>
		public int RunAsyncActionCount {
			get {
				return Volatile.Read(ref this._runAsyncActionCount);
			}
		}

		/// <inheritdoc />
		public void Run(Action action) {
			action();
		}

		/// <inheritdoc />
		public Task RunAsync(Action action) {
			Interlocked.Increment(ref this._runAsyncActionCount);
			action();
			return Task.CompletedTask;
		}

		/// <inheritdoc />
		public Task RunAsync(Func<Task> action) {
			return action();
		}
	}

	/// <summary>
	/// <see cref="CreateDbContextAsync"/> 実行時のスレッド ID を記録するテスト用ラッパーファクトリ。
	/// </summary>
	private sealed class TrackingDbContextFactory(IDbContextFactory<MediaDeckDbContext> inner) : IDbContextFactory<MediaDeckDbContext> {
		private readonly List<int> _dbQueryThreadIds = [];

		/// <summary>
		/// 記録されたマネージドスレッド ID 一覧を取得します。
		/// </summary>
		public IReadOnlyList<int> DbQueryThreadIds {
			get {
				lock (this._dbQueryThreadIds) {
					return [.. this._dbQueryThreadIds];
				}
			}
		}

		/// <inheritdoc />
		public MediaDeckDbContext CreateDbContext() {
			return inner.CreateDbContext();
		}

		/// <inheritdoc />
		public Task<MediaDeckDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) {
			lock (this._dbQueryThreadIds) {
				this._dbQueryThreadIds.Add(Environment.CurrentManagedThreadId);
			}
			return inner.CreateDbContextAsync(cancellationToken);
		}
	}
}