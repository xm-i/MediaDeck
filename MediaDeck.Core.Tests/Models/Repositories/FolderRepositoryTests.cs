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
/// <see cref="FolderRepository"/> の単体テスト。
/// </summary>
public sealed class FolderRepositoryTests : IDisposable {
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

	private static FolderRepository CreateSut(
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
		return new FolderRepository(dbFactory, dispatcherMock.Object, tabState, uiDispatcher ?? new RecordingUiDispatcher());
	}

	private static TabStateModel CreateTabState() {
		var services = new ServiceCollection();
		MediaDeck.Composition.DIRegistration.AddGeneratedServices(services);
		return services.BuildServiceProvider().GetRequiredService<TabStateModel>();
	}

	/// <summary>
	///     Loadで空のDBからルートフォルダが作成されることを確認します。
	/// </summary>
	[Fact]
	public async Task Load_EmptyDb_CreatesRootFolder() {
		var dbFactory = this.CreateSqliteDbFactory();
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
		var dbFactory = this.CreateSqliteDbFactory();

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
	///     同一データでLoadを2回呼ぶとキャッシュされ、UIディスパッチャーも再呼び出しされないことを確認します。
	/// </summary>
	[Fact]
	public async Task Load_CalledTwiceWithSameData_DoesNotRebuildTree() {
		var dbFactory = this.CreateSqliteDbFactory();
		var uiDispatcher = new RecordingUiDispatcher();
		using var sut = CreateSut(dbFactory, uiDispatcher);

		await sut.Load();
		var first = sut.RootFolder.Value;
		var countAfterFirst = uiDispatcher.RunAsyncActionCount;

		await sut.Load();
		var second = sut.RootFolder.Value;

		// 同じデータならオブジェクトは変わらない（SequenceEqualでスキップされる）
		first.ShouldBeSameAs(second);
		uiDispatcher.RunAsyncActionCount.ShouldBe(countAfterFirst);
	}

	/// <summary>
	///     Load が呼び出し元スレッドとは別のバックグラウンドスレッドで DB クエリを実行し、IUiDispatcher 経由で RootFolder を更新することを確認します。
	/// </summary>
	[Fact]
	public void Load_RunsDbQueryOffCallerThread_AndUpdatesRootFolderViaUiDispatcher() {
		var baseFactory = this.CreateSqliteDbFactory();
		using (var db = baseFactory.CreateDbContext()) {
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
		sut.RootFolder.Value.ShouldNotBeNull();
		sut.RootFolder.Value.ChildFolders.Length.ShouldBe(1);
	}

	/// <summary>
	///     FolderSearchCondition が設定されている場合、RootFolder 更新通知の時点で IsExpanded が既に復元されていることを確認します。
	/// </summary>
	[Fact]
	public async Task Load_WithFolderSearchCondition_RestoresIsExpandedBeforePublishingRootFolder() {
		var dbFactory = this.CreateSqliteDbFactory();
		var targetDir = $"C:{Path.DirectorySeparatorChar}Photos{Path.DirectorySeparatorChar}2024";
		var siblingDir = $"C:{Path.DirectorySeparatorChar}Photos{Path.DirectorySeparatorChar}2025";

		await using (var db = await dbFactory.CreateDbContextAsync()) {
			db.MediaItems.Add(new MediaItem {
				MediaItemId = 1,
				FilePath = $"{targetDir}{Path.DirectorySeparatorChar}img1.jpg",
				DirectoryPath = targetDir,
				MediaType = MediaType.Image,
				Description = "",
				IsUnderFolderGroup = false,
				ThumbnailFileName = "",
				FileSize = 100,
			});
			db.MediaItems.Add(new MediaItem {
				MediaItemId = 2,
				FilePath = $"{siblingDir}{Path.DirectorySeparatorChar}img2.jpg",
				DirectoryPath = siblingDir,
				MediaType = MediaType.Image,
				Description = "",
				IsUnderFolderGroup = false,
				ThumbnailFileName = "",
				FileSize = 100,
			});
			await db.SaveChangesAsync();
		}

		var tabState = CreateTabState();
		tabState.SearchState.SearchCondition.Add(new FolderSearchCondition {
			FolderPath = targetDir,
			IncludeSubDirectories = true,
		});

		using var sut = CreateSut(dbFactory, tabState: tabState);
		FolderObject? observedRoot = null;
		bool? observedParentExpanded = null;
		bool? observedTargetExpanded = null;
		using var subscription = sut.RootFolder
			.Where(root => root != null)
			.Subscribe(root => {
				observedRoot = root;
				var photos = root.ChildFolders.FirstOrDefault(x => x.FolderName.EndsWith("Photos", StringComparison.Ordinal));
				observedParentExpanded = photos?.IsExpanded;
				observedTargetExpanded = photos?.ChildFolders.FirstOrDefault(x => x.FolderName == "2024")?.IsExpanded;
			});

		await sut.Load();

		observedRoot.ShouldNotBeNull();
		observedParentExpanded.ShouldBe(true);
		observedTargetExpanded.ShouldBe(true);
	}

	/// <summary>
	///     Load を並行して複数回呼び出しても直列化され、競合せず最新のフォルダ構成が反映されることを確認します。
	/// </summary>
	[Fact]
	public async Task Load_CalledConcurrently_SerializesAndReflectsLatestState() {
		var baseFactory = this.CreateSqliteDbFactory();
		await using (var db = await baseFactory.CreateDbContextAsync()) {
			db.MediaItems.Add(new MediaItem {
				MediaItemId = 1,
				FilePath = $"C:{Path.DirectorySeparatorChar}Dir1{Path.DirectorySeparatorChar}img1.jpg",
				DirectoryPath = $"C:{Path.DirectorySeparatorChar}Dir1",
				MediaType = MediaType.Image,
				Description = "",
				IsUnderFolderGroup = false,
				ThumbnailFileName = "",
				FileSize = 100,
			});
			await db.SaveChangesAsync();
		}

		var gateFactory = new GatedFirstDbContextFactory(baseFactory);
		using var sut = CreateSut(gateFactory);

		var firstLoadTask = sut.Load();
		await gateFactory.FirstCallEntered.WaitAsync(TimeSpan.FromSeconds(5));

		await using (var db = await baseFactory.CreateDbContextAsync()) {
			db.MediaItems.Add(new MediaItem {
				MediaItemId = 2,
				FilePath = $"C:{Path.DirectorySeparatorChar}Dir2{Path.DirectorySeparatorChar}img2.jpg",
				DirectoryPath = $"C:{Path.DirectorySeparatorChar}Dir2",
				MediaType = MediaType.Image,
				Description = "",
				IsUnderFolderGroup = false,
				ThumbnailFileName = "",
				FileSize = 200,
			});
			await db.SaveChangesAsync();
		}

		var secondLoadTask = sut.Load();
		gateFactory.ReleaseFirstCall();

		await Task.WhenAll(firstLoadTask, secondLoadTask);

		sut.RootFolder.Value.ShouldNotBeNull();
		sut.RootFolder.Value.ChildFolders.Length.ShouldBe(1);
		sut.RootFolder.Value.ChildFolders[0].FolderPath.ShouldBe("C:");
		sut.RootFolder.Value.ChildFolders[0].ChildFolders.Length.ShouldBe(2);
	}

	/// <summary>
	///     Dispose 後に Load を呼び出しても UI ディスパッチャー呼び出しや RootFolder 更新が行われないことを確認します。
	/// </summary>
	[Fact]
	public async Task Load_WhenDisposed_SkipsUiUpdate() {
		var dbFactory = this.CreateSqliteDbFactory();
		var uiDispatcher = new RecordingUiDispatcher();
		var sut = CreateSut(dbFactory, uiDispatcher);

		sut.Dispose();
		await sut.Load();

		uiDispatcher.RunAsyncActionCount.ShouldBe(0);
		sut.RootFolder.Value.ShouldBeNull();
	}

	/// <summary>
	/// 同期的にコールバックを実行しつつ呼び出し回数を記録するテスト用 <see cref="IUiDispatcher"/> 実装。
	/// </summary>
	private sealed class RecordingUiDispatcher : IUiDispatcher {
		/// <summary>
		/// <see cref="RunAsync(Action)"/> が呼ばれた回数を取得します。
		/// </summary>
		public int RunAsyncActionCount {
			get {
				return Volatile.Read(ref this._runAsyncActionCount);
			}
		}

		private int _runAsyncActionCount;

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

	/// <summary>
	/// 初回の <see cref="CreateDbContextAsync"/> 呼び出しを一時停止させて並行呼び出しを制御するテスト用ファクトリ。
	/// </summary>
	private sealed class GatedFirstDbContextFactory(IDbContextFactory<MediaDeckDbContext> inner) : IDbContextFactory<MediaDeckDbContext> {
		private readonly TaskCompletionSource _firstCallEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
		private readonly TaskCompletionSource _releaseFirstCall = new(TaskCreationOptions.RunContinuationsAsynchronously);
		private int _callCount;

		/// <summary>
		/// 初回呼び出しに到達したことを待機するためのタスクを取得します。
		/// </summary>
		public Task FirstCallEntered {
			get {
				return this._firstCallEntered.Task;
			}
		}

		/// <summary>
		/// 待機中の初回呼び出しを解放します。
		/// </summary>
		public void ReleaseFirstCall() {
			this._releaseFirstCall.TrySetResult();
		}

		/// <inheritdoc />
		public MediaDeckDbContext CreateDbContext() {
			return inner.CreateDbContext();
		}

		/// <inheritdoc />
		public async Task<MediaDeckDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) {
			if (Interlocked.Increment(ref this._callCount) == 1) {
				this._firstCallEntered.TrySetResult();
				await this._releaseFirstCall.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
			}
			return await inner.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
		}
	}
}