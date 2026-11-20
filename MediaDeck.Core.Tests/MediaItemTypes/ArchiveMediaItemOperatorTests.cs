using System.IO.Compression;
using ImageMagick;
using MediaDeck.Composition.Database;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.Services;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Composition.Stores.Config.Model.Objects;
using MediaDeck.Composition.Stores.State.Model.Objects;
using MediaDeck.Composition.Tables;
using MediaDeck.MediaItemTypes.Archive.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;

namespace MediaDeck.Core.Tests.MediaItemTypes;

/// <summary>
/// <see cref="ArchiveMediaItemOperator"/> の単体テスト。
/// </summary>
public sealed class ArchiveMediaItemOperatorTests : IDisposable {
	private readonly string _tempDir;
	private readonly SqliteConnection _connection;
	private readonly DbContextOptions<MediaDeckDbContext> _dbOptions;
	private readonly ServiceProvider _serviceProvider;
	private readonly ArchiveMediaItemOperator _sut;

	/// <summary>
	/// テスト用の一時ディレクトリ・インメモリSQLite・モック依存関係を初期化します。
	/// </summary>
	public ArchiveMediaItemOperatorTests() {
		this._tempDir = Path.Combine(Path.GetTempPath(), $"MediaDeck_ArchiveTests_{Guid.NewGuid():N}");
		Directory.CreateDirectory(this._tempDir);

		this._connection = new SqliteConnection("DataSource=:memory:");
		this._connection.Open();
		this._dbOptions = new DbContextOptionsBuilder<MediaDeckDbContext>()
			.UseSqlite(this._connection)
			.Options;
		using (var db = new MediaDeckDbContext(this._dbOptions)) {
			db.Database.EnsureCreated();
		}

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
		services.AddSingleton<IStringProvider, StubStringProvider>();
		services.AddSingleton<ConfigModel>();
		services.AddTransient<SortObject>(sp => new SortObject(sp));
		services.AddTransient<SortItemObject>();
		this._serviceProvider = services.BuildServiceProvider();

		var config = this._serviceProvider.GetRequiredService<ConfigModel>();

		var filePathService = new Mock<IFilePathService>();
		filePathService
			.Setup(x => x.GetThumbnailRelativeFilePath())
			.Returns(() => $"{Guid.NewGuid():N}.jpg");
		filePathService
			.Setup(x => x.GetThumbnailAbsoluteFilePath(It.IsAny<string>()))
			.Returns<string>(rel => Path.Combine(this._tempDir, "thumbs", rel));

		var dbFactory = new Mock<IDbContextFactory<MediaDeckDbContext>>();
		dbFactory
			.Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
			.Returns(() => Task.FromResult(new MediaDeckDbContext(this._dbOptions)));

		var hashService = new Mock<IFileHashUpdatorService>();

		var coordinator = new Mock<IDatabaseWriteCoordinator>();
		coordinator
			.Setup(x => x.ExecuteAsync(It.IsAny<Func<CancellationToken, Task<MediaItem?>>>(), It.IsAny<CancellationToken>()))
			.Returns((Func<CancellationToken, Task<MediaItem?>> op, CancellationToken ct) => op(ct));

		var mediaTypeService = new Mock<IMediaItemTypeService>();
		mediaTypeService
			.Setup(x => x.IsTargetPath(It.IsAny<string>(), MediaType.Image))
			.Returns<string, MediaType>((name, _) => name.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase));

		var mockServiceProvider = new Mock<IServiceProvider>();
		mockServiceProvider
			.Setup(x => x.GetService(typeof(IMediaItemTypeService)))
			.Returns(mediaTypeService.Object);

		var logger = new Mock<ILogger<ArchiveMediaItemOperator>>();

		this._sut = new ArchiveMediaItemOperator(
			filePathService.Object,
			logger.Object,
			dbFactory.Object,
			hashService.Object,
			coordinator.Object,
			mockServiceProvider.Object,
			config);
	}

	/// <summary>
	/// テストで使用したリソースを解放します。
	/// </summary>
	public void Dispose() {
		this._serviceProvider.Dispose();
		this._connection.Dispose();
		if (Directory.Exists(this._tempDir)) {
			Directory.Delete(this._tempDir, recursive: true);
		}
	}

	private string CreateSampleZip(string fileName) {
		var zipPath = Path.Combine(this._tempDir, fileName);
		using var image = new MagickImage(MagickColors.Red, 1, 1) { Format = MagickFormat.Jpg };
		var imageBytes = image.ToByteArray();

		using var fs = File.Create(zipPath);
		using var archive = new ZipArchive(fs, ZipArchiveMode.Create);
		var entry = archive.CreateEntry("page1.jpg");
		using var entryStream = entry.Open();
		entryStream.Write(imageBytes, 0, imageBytes.Length);
		return zipPath;
	}

	/// <summary>
	/// 有効な ZIP アーカイブを登録した際、画像寸法・ページ数・サムネイル情報が設定されてDBに保存されることを検証します。
	/// </summary>
	[Fact]
	public async Task RegisterMediaItemAsync_WithValidZipArchive_ShouldRegisterItemWithDimensionsAndPageCount() {
		var zipPath = this.CreateSampleZip("sample.zip");

		var registered = await this._sut.RegisterMediaItemAsync(zipPath);

		registered.ShouldNotBeNull();
		registered.MediaType.ShouldBe(MediaType.Archive);
		registered.Width.ShouldBe(1);
		registered.Height.ShouldBe(1);
		registered.Metadata.ShouldNotBeNull();
		registered.Metadata.Entries.ShouldContain(x => x.Key == "PageCount" && x.Value == "1");

		await using var db = new MediaDeckDbContext(this._dbOptions);
		(await db.MediaItems.CountAsync()).ShouldBe(1);
	}

	/// <summary>
	/// 有効な ZIP アーカイブに対して <see cref="ArchiveMediaItemOperator.UpdateMetadata(MediaItem)"/> を実行した際、
	/// PageCount メタデータが最新値で更新されることを検証します。
	/// </summary>
	[Fact]
	public async Task UpdateMetadata_WithValidZipArchive_ShouldUpdatePageCount() {
		var zipPath = this.CreateSampleZip("update.zip");
		var mediaItem = new MediaItem {
			MediaType = MediaType.Archive,
			FilePath = zipPath,
			DirectoryPath = this._tempDir,
			Description = string.Empty,
			IsUnderFolderGroup = false
		};

		await this._sut.UpdateMetadata(mediaItem);

		mediaItem.Metadata.ShouldNotBeNull();
		mediaItem.Metadata.Entries.ShouldContain(x => x.Key == "PageCount" && x.Value == "1");
	}

	/// <summary>
	/// 存在しないファイルに対して <see cref="ArchiveMediaItemOperator.UpdateMetadata(MediaItem)"/> を実行しても
	/// 例外を送出せず終了することを検証します。
	/// </summary>
	[Fact]
	public async Task UpdateMetadata_WhenFileNotFound_ShouldNotThrow() {
		var missingPath = Path.Combine(this._tempDir, "missing.zip");
		var mediaItem = new MediaItem {
			MediaType = MediaType.Archive,
			FilePath = missingPath,
			DirectoryPath = this._tempDir,
			Description = string.Empty,
			IsUnderFolderGroup = false
		};

		await Should.NotThrowAsync(() => this._sut.UpdateMetadata(mediaItem));
	}
}