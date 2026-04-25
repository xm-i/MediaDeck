using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;
using MediaDeck.Composition.Objects;
using MediaDeck.Core.Models.Files;
using MediaDeck.Core.Models.NotificationDispatcher;
using MediaDeck.Database;
using MediaDeck.Database.Tables;

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
			context.MediaItems.Add(new MediaItem { MediaItemId = 1, FilePath = "path1.jpg", DirectoryPath = "dir", Description = "" });
			context.MediaItems.Add(new MediaItem { MediaItemId = 2, FilePath = "path2.jpg", DirectoryPath = "dir", Description = "" });
			context.MediaItems.Add(new MediaItem { MediaItemId = 3, FilePath = "path3.jpg", DirectoryPath = "dir", Description = "" });
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
			context.MediaItems.Add(new MediaItem { MediaItemId = 1, FilePath = "path1.jpg", DirectoryPath = "dir", Description = "" });
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
			context.MediaItems.Add(new MediaItem { MediaItemId = 1, FilePath = "path1.jpg", DirectoryPath = "dir", Description = "" });
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
			context.MediaItems.Add(new MediaItem { MediaItemId = 1, FilePath = "path1.jpg", DirectoryPath = "dir", Description = "" });
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
}