using MediaDeck.Composition.Database;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;
using MediaDeck.Composition.Tables;
using MediaDeck.Core.Services.MediaItemMetadataUpdator;
using Microsoft.EntityFrameworkCore;
using Moq;
using R3;
using Shouldly;

namespace MediaDeck.Core.Tests.Services.MediaItemMetadataUpdator;

/// <summary>
/// <see cref="MediaItemMetadataUpdatorService"/> のユニットテスト。
/// </summary>
public class MediaItemMetadataUpdatorServiceTests {
	private IDbContextFactory<MediaDeckDbContext> CreateInMemoryDbFactory(string dbName) {
		var options = new DbContextOptionsBuilder<MediaDeckDbContext>()
			.UseInMemoryDatabase(databaseName: dbName)
			.ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
			.Options;

		var factoryMock = new Mock<IDbContextFactory<MediaDeckDbContext>>();
		factoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(() => new MediaDeckDbContext(options));

		return factoryMock.Object;
	}

	/// <summary>
	/// 全件更新が正常に全アイテムを処理することを検証します。
	/// </summary>
	[Fact]
	public async Task UpdateMetadataAsync_ShouldUpdateAllItems() {
		var dbName = nameof(this.UpdateMetadataAsync_ShouldUpdateAllItems);
		var dbFactory = this.CreateInMemoryDbFactory(dbName);
		await using (var db = await dbFactory.CreateDbContextAsync()) {
			db.MediaItems.Add(new MediaItem { MediaItemId = 1, MediaType = MediaType.Image, FilePath = "test1", DirectoryPath = "dir", Description = "", IsUnderFolderGroup = false });
			db.MediaItems.Add(new MediaItem { MediaItemId = 2, MediaType = MediaType.Image, FilePath = "test2", DirectoryPath = "dir", Description = "", IsUnderFolderGroup = false });
			await db.SaveChangesAsync();
		}

		var operatorMock = new Mock<IMediaItemOperator>();
		operatorMock.SetupGet(x => x.TargetMediaType).Returns(MediaType.Image);

		var typeServiceMock = new Mock<IMediaItemTypeService>();
		typeServiceMock.Setup(x => x.CreateMediaItemOperators()).Returns([operatorMock.Object]);
		typeServiceMock.Setup(x => x.IncludeTables(It.IsAny<IQueryable<MediaItem>>())).Returns((IQueryable<MediaItem> q) => q);

		var service = new MediaItemMetadataUpdatorService(dbFactory, typeServiceMock.Object);

		await service.UpdateMetadataAsync();

		operatorMock.Verify(x => x.UpdateMetadata(It.IsAny<MediaItem>()), Times.Exactly(2));
		service.CompletedCount.Value.ShouldBe(2);
		service.TargetCount.Value.ShouldBe(2);
	}

	/// <summary>
	/// 指定したIDリストのみが更新されることを検証します。
	/// </summary>
	[Fact]
	public async Task UpdateMetadataAsync_WithIds_ShouldUpdateOnlySpecificItems() {
		var dbName = nameof(this.UpdateMetadataAsync_WithIds_ShouldUpdateOnlySpecificItems);
		var dbFactory = this.CreateInMemoryDbFactory(dbName);
		await using (var db = await dbFactory.CreateDbContextAsync()) {
			db.MediaItems.Add(new MediaItem { MediaItemId = 1, MediaType = MediaType.Image, FilePath = "test1", DirectoryPath = "dir", Description = "", IsUnderFolderGroup = false });
			db.MediaItems.Add(new MediaItem { MediaItemId = 2, MediaType = MediaType.Image, FilePath = "test2", DirectoryPath = "dir", Description = "", IsUnderFolderGroup = false });
			await db.SaveChangesAsync();
		}

		var operatorMock = new Mock<IMediaItemOperator>();
		operatorMock.SetupGet(x => x.TargetMediaType).Returns(MediaType.Image);

		var typeServiceMock = new Mock<IMediaItemTypeService>();
		typeServiceMock.Setup(x => x.CreateMediaItemOperators()).Returns([operatorMock.Object]);
		typeServiceMock.Setup(x => x.IncludeTables(It.IsAny<IQueryable<MediaItem>>())).Returns((IQueryable<MediaItem> q) => q);

		var service = new MediaItemMetadataUpdatorService(dbFactory, typeServiceMock.Object);

		await service.UpdateMetadataAsync([1]);

		operatorMock.Verify(x => x.UpdateMetadata(It.Is<MediaItem>(m => m.MediaItemId == 1)), Times.Once);
		operatorMock.Verify(x => x.UpdateMetadata(It.Is<MediaItem>(m => m.MediaItemId == 2)), Times.Never);
		service.CompletedCount.Value.ShouldBe(1);
		service.TargetCount.Value.ShouldBe(1);
	}

	/// <summary>
	/// キャンセルトークンによって処理が中断されることを検証します。
	/// </summary>
	[Fact]
	public async Task UpdateMetadataAsync_ShouldRespectCancellation() {
		var dbName = nameof(this.UpdateMetadataAsync_ShouldRespectCancellation);
		var dbFactory = this.CreateInMemoryDbFactory(dbName);
		await using (var db = await dbFactory.CreateDbContextAsync()) {
			for (int i = 1; i <= 10; i++) {
				db.MediaItems.Add(new MediaItem { MediaItemId = i, MediaType = MediaType.Image, FilePath = $"test{i}", DirectoryPath = "dir", Description = "", IsUnderFolderGroup = false });
			}
			await db.SaveChangesAsync();
		}

		var operatorMock = new Mock<IMediaItemOperator>();
		operatorMock.SetupGet(x => x.TargetMediaType).Returns(MediaType.Image);
		operatorMock.Setup(x => x.UpdateMetadata(It.IsAny<MediaItem>())).Returns(Task.CompletedTask);

		var typeServiceMock = new Mock<IMediaItemTypeService>();
		typeServiceMock.Setup(x => x.CreateMediaItemOperators()).Returns([operatorMock.Object]);
		typeServiceMock.Setup(x => x.IncludeTables(It.IsAny<IQueryable<MediaItem>>())).Returns((IQueryable<MediaItem> q) => q);

		var service = new MediaItemMetadataUpdatorService(dbFactory, typeServiceMock.Object);

		var cts = new CancellationTokenSource();
		// 1つ目の更新が終わったらキャンセルする
		using var d = service.CompletedCount.Subscribe(c => {
			if (c == 1) {
				cts.Cancel();
			}
		});

		await service.UpdateMetadataAsync(cts.Token);

		// キャンセルされたので、全10件は更新されない
		service.CompletedCount.Value.ShouldBeLessThan(10);
	}

	/// <summary>
	/// 連続して更新リクエストが来た場合に、ターゲット件数が加算されることを検証します。
	/// </summary>
	[Fact]
	public async Task TargetCount_ShouldBeAdditive_WhenMultipleRequestsAreMade() {
		var dbName = nameof(this.TargetCount_ShouldBeAdditive_WhenMultipleRequestsAreMade);
		var dbFactory = this.CreateInMemoryDbFactory(dbName);
		await using (var db = await dbFactory.CreateDbContextAsync()) {
			db.MediaItems.Add(new MediaItem { MediaItemId = 1, MediaType = MediaType.Image, FilePath = "test1", DirectoryPath = "dir", Description = "", IsUnderFolderGroup = false });
			db.MediaItems.Add(new MediaItem { MediaItemId = 2, MediaType = MediaType.Image, FilePath = "test2", DirectoryPath = "dir", Description = "", IsUnderFolderGroup = false });
			await db.SaveChangesAsync();
		}

		var operatorMock = new Mock<IMediaItemOperator>();
		operatorMock.SetupGet(x => x.TargetMediaType).Returns(MediaType.Image);

		// 1件目の処理中に2件目のリクエストを投げるために待機させる
		var tcs = new TaskCompletionSource();
		operatorMock.Setup(x => x.UpdateMetadata(It.Is<MediaItem>(m => m.MediaItemId == 1))).Returns(tcs.Task);

		var typeServiceMock = new Mock<IMediaItemTypeService>();
		typeServiceMock.Setup(x => x.CreateMediaItemOperators()).Returns([operatorMock.Object]);
		typeServiceMock.Setup(x => x.IncludeTables(It.IsAny<IQueryable<MediaItem>>())).Returns((IQueryable<MediaItem> q) => q);

		var service = new MediaItemMetadataUpdatorService(dbFactory, typeServiceMock.Object);

		// 1回目のリクエスト（非同期で開始）
		var task1 = service.UpdateMetadataAsync([1]);

		service.TargetCount.Value.ShouldBe(1);

		// 2回目のリクエスト
		var task2 = service.UpdateMetadataAsync([2]);

		// ターゲット件数が 1 + 1 = 2 になっているはず
		service.TargetCount.Value.ShouldBe(2);

		// 処理を完了させる
		tcs.SetResult();
		await Task.WhenAll(task1, task2);

		service.CompletedCount.Value.ShouldBe(2);
	}

	/// <summary>
	/// 一部のファイルでメタデータ更新時に例外が発生しても、他のファイルが正常に処理され完了数がインクリメントされることを検証します。
	/// </summary>
	[Fact]
	public async Task UpdateMetadataAsync_ShouldContinueProcessing_WhenOneItemFails() {
		var dbName = nameof(this.UpdateMetadataAsync_ShouldContinueProcessing_WhenOneItemFails);
		var dbFactory = this.CreateInMemoryDbFactory(dbName);
		await using (var db = await dbFactory.CreateDbContextAsync()) {
			db.MediaItems.Add(new MediaItem { MediaItemId = 1, MediaType = MediaType.Image, FilePath = "test1", DirectoryPath = "dir", Description = "", IsUnderFolderGroup = false });
			db.MediaItems.Add(new MediaItem { MediaItemId = 2, MediaType = MediaType.Image, FilePath = "test2", DirectoryPath = "dir", Description = "", IsUnderFolderGroup = false });
			await db.SaveChangesAsync();
		}

		var operatorMock = new Mock<IMediaItemOperator>();
		operatorMock.SetupGet(x => x.TargetMediaType).Returns(MediaType.Image);
		// 1件目は例外をスローし、2件目は正常終了する
		operatorMock.Setup(x => x.UpdateMetadata(It.Is<MediaItem>(m => m.MediaItemId == 1)))
			.ThrowsAsync(new InvalidOperationException("Corrupted file"));
		operatorMock.Setup(x => x.UpdateMetadata(It.Is<MediaItem>(m => m.MediaItemId == 2)))
			.Returns(Task.CompletedTask);

		var typeServiceMock = new Mock<IMediaItemTypeService>();
		typeServiceMock.Setup(x => x.CreateMediaItemOperators()).Returns([operatorMock.Object]);
		typeServiceMock.Setup(x => x.IncludeTables(It.IsAny<IQueryable<MediaItem>>())).Returns((IQueryable<MediaItem> q) => q);

		var service = new MediaItemMetadataUpdatorService(dbFactory, typeServiceMock.Object);

		await service.UpdateMetadataAsync([1, 2]);

		operatorMock.Verify(x => x.UpdateMetadata(It.Is<MediaItem>(m => m.MediaItemId == 1)), Times.Once);
		operatorMock.Verify(x => x.UpdateMetadata(It.Is<MediaItem>(m => m.MediaItemId == 2)), Times.Once);
		service.CompletedCount.Value.ShouldBe(2);
		service.TargetCount.Value.ShouldBe(2);
	}
}