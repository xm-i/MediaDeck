using MediaDeck.Composition.Database;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;
using MediaDeck.Composition.Objects;
using MediaDeck.Composition.Tables;
using MediaDeck.Core.Models.Files;
using MediaDeck.Core.Models.NotificationDispatcher;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using R3;
using Shouldly;

namespace MediaDeck.Core.Tests.Models.FileDetailManagers;

/// <summary>
/// FilesManagerクラスのテスト
/// </summary>
public class FilesManagerTests : IDisposable {
	private readonly SqliteConnection _connection;
	private readonly Mock<IDbContextFactory<MediaDeckDbContext>> _dbFactoryMock;
	private readonly AppNotificationDispatcher _notificationDispatcher;
	private readonly FilesManager _filesManager;
	private readonly List<AppNotification> _notifications;
	private readonly IDisposable? _notificationSubscription;

	/// <summary>
	/// テストの初期化設定
	/// </summary>
	public FilesManagerTests() {
		// SQLiteインメモリデータベースの設定
		this._connection = new SqliteConnection("DataSource=:memory:");
		this._connection.Open();

		var options = new DbContextOptionsBuilder<MediaDeckDbContext>()
			.UseSqlite(this._connection)
			.Options;

		using (var context = new MediaDeckDbContext(options)) {
			context.Database.EnsureCreated();
		}

		this._dbFactoryMock = new Mock<IDbContextFactory<MediaDeckDbContext>>();
		this._dbFactoryMock.Setup(f => f.CreateDbContextAsync(default))
			.ReturnsAsync(() => new MediaDeckDbContext(options));

		this._notificationDispatcher = new AppNotificationDispatcher();
		this._notifications = new List<AppNotification>();
		this._notificationSubscription = this._notificationDispatcher.Notify.Subscribe(n => this._notifications.Add(n));

		this._filesManager = new FilesManager(this._dbFactoryMock.Object, this._notificationDispatcher);
	}

	/// <summary>
	/// リソースの破棄
	/// </summary>
	public void Dispose() {
		this._notificationSubscription?.Dispose();
		this._connection.Dispose();
	}

	/// <summary>
	/// 正常系：複数のファイルが指定され、DBに存在する場合、それらを削除して複数ファイル用の通知を発行することを検証する
	/// </summary>
	[Fact]
	public async Task RemoveFilesAsync_WhenFilesExist_ShouldRemoveFilesAndNotify() {
		// Arrange
		await using (var context = await this._dbFactoryMock.Object.CreateDbContextAsync()) {
			context.MediaItems.Add(new MediaItem { MediaType = MediaType.Image, MediaItemId = 1, FilePath = "path1.jpg", DirectoryPath = "dir", Description = "", IsUnderFolderGroup = false });
			context.MediaItems.Add(new MediaItem { MediaType = MediaType.Image, MediaItemId = 2, FilePath = "path2.jpg", DirectoryPath = "dir", Description = "", IsUnderFolderGroup = false });
			context.MediaItems.Add(new MediaItem { MediaType = MediaType.Image, MediaItemId = 3, FilePath = "path3.jpg", DirectoryPath = "dir", Description = "", IsUnderFolderGroup = false });
			await context.SaveChangesAsync();
		}

		var file1Mock = new Mock<IMediaItemModel>();
		file1Mock.Setup(m => m.Id).Returns(1);
		var file2Mock = new Mock<IMediaItemModel>();
		file2Mock.Setup(m => m.Id).Returns(2);

		var models = new List<IMediaItemModel> { file1Mock.Object, file2Mock.Object };

		// Act
		await this._filesManager.RemoveFilesAsync(models);

		// Assert
		await using (var context = await this._dbFactoryMock.Object.CreateDbContextAsync()) {
			var files = await context.MediaItems.ToListAsync();
			files.Count.ShouldBe(1);
			files.Single().MediaItemId.ShouldBe(3);
		}

		this._notifications.Count.ShouldBe(1);
		this._notifications[0].Message.ShouldBe("2 files removed from MediaDeck database");
	}

	/// <summary>
	/// 正常系：単一のファイルが指定され、DBに存在する場合、それを削除して単一ファイル用の通知を発行することを検証する
	/// </summary>
	[Fact]
	public async Task RemoveFilesAsync_WhenSingleFileExists_ShouldRemoveAndNotifySingleMessage() {
		// Arrange
		await using (var context = await this._dbFactoryMock.Object.CreateDbContextAsync()) {
			context.MediaItems.Add(new MediaItem { MediaType = MediaType.Image, MediaItemId = 1, FilePath = "path1.jpg", DirectoryPath = "dir", Description = "", IsUnderFolderGroup = false });
			await context.SaveChangesAsync();
		}

		var fileMock = new Mock<IMediaItemModel>();
		fileMock.Setup(m => m.Id).Returns(1);

		var models = new List<IMediaItemModel> { fileMock.Object };

		// Act
		await this._filesManager.RemoveFilesAsync(models);

		// Assert
		await using (var context = await this._dbFactoryMock.Object.CreateDbContextAsync()) {
			var files = await context.MediaItems.ToListAsync();
			files.ShouldBeEmpty();
		}

		this._notifications.Count.ShouldBe(1);
		this._notifications[0].Message.ShouldBe("File removed from MediaDeck database");
	}

	/// <summary>
	/// 異常系：指定されたIDのファイルがDBに存在しない場合、例外を投げずDBも変更せず、通知も発行しないことを検証する
	/// </summary>
	[Fact]
	public async Task RemoveFilesAsync_WhenFileNotFound_ShouldNotThrowAndNotNotify() {
		// Arrange
		await using (var context = await this._dbFactoryMock.Object.CreateDbContextAsync()) {
			context.MediaItems.Add(new MediaItem { MediaType = MediaType.Image, MediaItemId = 1, FilePath = "path1.jpg", DirectoryPath = "dir", Description = "", IsUnderFolderGroup = false });
			await context.SaveChangesAsync();
		}

		var fileMock = new Mock<IMediaItemModel>();
		fileMock.Setup(m => m.Id).Returns(999); // 存在しないID

		var models = new List<IMediaItemModel> { fileMock.Object };

		// Act
		await Should.NotThrowAsync(() => this._filesManager.RemoveFilesAsync(models));

		// Assert
		await using (var context = await this._dbFactoryMock.Object.CreateDbContextAsync()) {
			var files = await context.MediaItems.ToListAsync();
			files.Count.ShouldBe(1);
		}

		this._notifications.ShouldBeEmpty();
	}

	/// <summary>
	/// 境界値：空のリストが渡された場合、例外を投げずDBも変更せず、通知も発行しないことを検証する
	/// </summary>
	[Fact]
	public async Task RemoveFilesAsync_WhenEmptyListProvided_ShouldNotThrowAndNotNotify() {
		// Arrange
		await using (var context = await this._dbFactoryMock.Object.CreateDbContextAsync()) {
			context.MediaItems.Add(new MediaItem { MediaType = MediaType.Image, MediaItemId = 1, FilePath = "path1.jpg", DirectoryPath = "dir", Description = "", IsUnderFolderGroup = false });
			await context.SaveChangesAsync();
		}

		var models = new List<IMediaItemModel>(); // 空のリスト

		// Act
		await Should.NotThrowAsync(() => this._filesManager.RemoveFilesAsync(models));

		// Assert
		await using (var context = await this._dbFactoryMock.Object.CreateDbContextAsync()) {
			var files = await context.MediaItems.ToListAsync();
			files.Count.ShouldBe(1);
		}

		this._notifications.ShouldBeEmpty();
	}

	/// <summary>
	/// 異常系：nullが渡された場合、ArgumentNullExceptionを投げることを検証する
	/// </summary>
	[Fact]
	public async Task RemoveFilesAsync_WhenNullProvided_ShouldThrowArgumentNullException() {
		// Arrange
		IEnumerable<IMediaItemModel> models = null!;

		// Act & Assert
		await Should.ThrowAsync<ArgumentNullException>(() => this._filesManager.RemoveFilesAsync(models));
	}

	/// <summary>
	/// 正常系：FolderGroup削除時に他に親FolderGroupが存在しない場合、配下アイテムのIsUnderFolderGroupがfalseに更新されることを検証する
	/// </summary>
	[Fact]
	public async Task RemoveFilesAsync_WhenFolderGroupRemovedAndNoParentRemains_ShouldClearIsUnderFolderGroup() {
		// Arrange
		var groupPath = Path.Combine("root", "groupA");
		var nestedDirPath = Path.Combine(groupPath, "nested");
		await using (var context = await this._dbFactoryMock.Object.CreateDbContextAsync()) {
			context.MediaItems.Add(new MediaItem { MediaType = MediaType.FolderGroup, MediaItemId = 1, FilePath = groupPath, DirectoryPath = "root", Description = "", IsUnderFolderGroup = false });
			context.MediaItems.Add(new MediaItem { MediaType = MediaType.Image, MediaItemId = 2, FilePath = Path.Combine(groupPath, "img1.jpg"), DirectoryPath = groupPath, Description = "", IsUnderFolderGroup = true });
			context.MediaItems.Add(new MediaItem { MediaType = MediaType.Image, MediaItemId = 3, FilePath = Path.Combine(nestedDirPath, "img2.jpg"), DirectoryPath = nestedDirPath, Description = "", IsUnderFolderGroup = true });
			await context.SaveChangesAsync();
		}

		var groupMock = new Mock<IMediaItemModel>();
		groupMock.Setup(m => m.Id).Returns(1);

		// Act
		await this._filesManager.RemoveFilesAsync([groupMock.Object]);

		// Assert
		await using (var context = await this._dbFactoryMock.Object.CreateDbContextAsync()) {
			var remaining = await context.MediaItems.OrderBy(x => x.MediaItemId).ToListAsync();
			remaining.Count.ShouldBe(2);
			remaining.ShouldAllBe(x => !x.IsUnderFolderGroup);
		}
	}

	/// <summary>
	/// 正常系：入れ子のFolderGroup削除時に上位の親FolderGroupが残存している場合、配下アイテムのIsUnderFolderGroupがtrueのまま維持されることを検証する
	/// </summary>
	[Fact]
	public async Task RemoveFilesAsync_WhenNestedFolderGroupRemovedAndParentRemains_ShouldKeepIsUnderFolderGroupTrue() {
		// Arrange
		var parentGroupPath = Path.Combine("root", "parent");
		var childGroupPath = Path.Combine(parentGroupPath, "child");
		await using (var context = await this._dbFactoryMock.Object.CreateDbContextAsync()) {
			context.MediaItems.Add(new MediaItem { MediaType = MediaType.FolderGroup, MediaItemId = 1, FilePath = parentGroupPath, DirectoryPath = "root", Description = "", IsUnderFolderGroup = false });
			context.MediaItems.Add(new MediaItem { MediaType = MediaType.FolderGroup, MediaItemId = 2, FilePath = childGroupPath, DirectoryPath = parentGroupPath, Description = "", IsUnderFolderGroup = true });
			context.MediaItems.Add(new MediaItem { MediaType = MediaType.Image, MediaItemId = 3, FilePath = Path.Combine(childGroupPath, "img1.jpg"), DirectoryPath = childGroupPath, Description = "", IsUnderFolderGroup = true });
			await context.SaveChangesAsync();
		}

		var childGroupMock = new Mock<IMediaItemModel>();
		childGroupMock.Setup(m => m.Id).Returns(2);

		// Act
		await this._filesManager.RemoveFilesAsync([childGroupMock.Object]);

		// Assert
		await using (var context = await this._dbFactoryMock.Object.CreateDbContextAsync()) {
			var img = await context.MediaItems.SingleAsync(x => x.MediaItemId == 3);
			img.IsUnderFolderGroup.ShouldBeTrue();
		}
	}
}