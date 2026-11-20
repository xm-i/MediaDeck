using ImageMagick;
using MediaDeck.Composition.Database;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Composition.Interfaces.Services;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Composition.Stores.Config.Model.Objects;
using MediaDeck.Composition.Stores.State.Model.Objects;
using MediaDeck.Composition.Tables;
using MediaDeck.MediaItemTypes.Image.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;

namespace MediaDeck.Core.Tests.MediaItemTypes;

/// <summary>
/// <see cref="ImageMediaItemOperator"/> の単体テスト。
/// </summary>
public sealed class ImageMediaItemOperatorTests : IDisposable {
	private readonly string _tempDir;
	private readonly SqliteConnection _connection;
	private readonly DbContextOptions<MediaDeckDbContext> _dbOptions;
	private readonly ServiceProvider _serviceProvider;
	private readonly ImageMediaItemOperator _sut;

	/// <summary>
	/// テスト用の一時ディレクトリ・インメモリSQLite・モック依存関係を初期化します。
	/// </summary>
	public ImageMediaItemOperatorTests() {
		this._tempDir = Path.Combine(Path.GetTempPath(), $"MediaDeck_ImageTests_{Guid.NewGuid():N}");
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

		var logger = new Mock<ILogger<ImageMediaItemOperator>>();

		this._sut = new ImageMediaItemOperator(
			filePathService.Object,
			config,
			dbFactory.Object,
			hashService.Object,
			coordinator.Object,
			logger.Object);
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

	private string CreateJpegWithGps(string fileName) {
		var path = Path.Combine(this._tempDir, fileName);
		using var image = new MagickImage(MagickColors.Blue, 1, 1) { Format = MagickFormat.Jpg };
		var profile = new ExifProfile();
		profile.SetValue(ExifTag.GPSLatitudeRef, "N");
		profile.SetValue(ExifTag.GPSLatitude, [new Rational(35, 1), new Rational(40, 1), new Rational(52, 1)]);
		profile.SetValue(ExifTag.GPSLongitudeRef, "E");
		profile.SetValue(ExifTag.GPSLongitude, [new Rational(139, 1), new Rational(46, 1), new Rational(0, 1)]);
		image.SetProfile(profile);
		image.Write(path);
		return path;
	}

	/// <summary>
	/// 同一 GPS 座標を持つ複数画像を登録した際、Positions レコードが重複追加されず既存レコードが再利用されることを検証します。
	/// </summary>
	[Fact]
	public async Task RegisterMediaItemAsync_WithSameGpsCoordinates_ShouldReuseExistingPosition() {
		var img1Path = this.CreateJpegWithGps("img1.jpg");
		var img2Path = this.CreateJpegWithGps("img2.jpg");

		var item1 = await this._sut.RegisterMediaItemAsync(img1Path);
		item1.ShouldNotBeNull();
		item1.MediaType.ShouldBe(MediaType.Image);
		item1.Latitude.ShouldNotBeNull();
		item1.Longitude.ShouldNotBeNull();

		await using (var db = new MediaDeckDbContext(this._dbOptions)) {
			(await db.Positions.CountAsync()).ShouldBe(1);
		}

		var item2 = await this._sut.RegisterMediaItemAsync(img2Path);
		item2.ShouldNotBeNull();

		await using (var db = new MediaDeckDbContext(this._dbOptions)) {
			(await db.MediaItems.CountAsync()).ShouldBe(2);
			(await db.Positions.CountAsync()).ShouldBe(1);
		}
	}
}