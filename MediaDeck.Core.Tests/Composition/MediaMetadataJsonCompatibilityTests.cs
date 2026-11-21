using MediaDeck.Composition.Database;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Composition.Tables;
using MediaDeck.Composition.Tables.Metadata;
using MediaDeck.MediaItemTypes.Archive;
using MediaDeck.MediaItemTypes.Image;
using MediaDeck.MediaItemTypes.Pdf;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace MediaDeck.Core.Tests.Composition;

/// <summary>
/// <see cref="MediaItem.Metadata"/> の <c>ComplexProperty</c> / <c>ComplexCollection</c> マッピングにおける
/// 既存 JSON カラム互換性・非推奨警告解消・プロバイダー結合を検証するテスト。
/// </summary>
public sealed class MediaMetadataJsonCompatibilityTests : IDisposable {
	private readonly SqliteConnection _connection;
	private readonly DbContextOptions<MediaDeckDbContext> _dbOptions;
	private readonly List<LogEntry> _warningLogs = [];
	private readonly LoggerFactory _loggerFactory;

	/// <summary>
	/// テスト用のインメモリ SQLite 接続と警告キャプチャ用ロガーを初期化します。
	/// </summary>
	public MediaMetadataJsonCompatibilityTests() {
		this._connection = new SqliteConnection("DataSource=:memory:");
		this._connection.Open();

		this._loggerFactory = new LoggerFactory([new CaptureWarningLoggerProvider(this._warningLogs)]);
		this._dbOptions = new DbContextOptionsBuilder<MediaDeckDbContext>()
			.UseSqlite(this._connection)
			.UseLoggerFactory(this._loggerFactory)
			.Options;

		using var db = new MediaDeckDbContext(this._dbOptions);
		db.Database.EnsureCreated();
	}

	/// <summary>
	/// テストで使用した接続とロガーファクトリを解放します。
	/// </summary>
	public void Dispose() {
		this._loggerFactory.Dispose();
		this._connection.Dispose();
	}

	/// <summary>
	/// 旧 <c>OwnsOne</c> + <c>OwnsMany</c> 形式で保存された生 JSON データ（NULL、空配列、重複キー、日本語・エスケープ文字含む）を
	/// 現行モデルが欠落なく読み込めることを検証します。
	/// </summary>
	[Fact]
	public async Task Load_WithLegacyJsonFormats_RestoresMetadataCorrectly() {
		await using (var db = new MediaDeckDbContext(this._dbOptions)) {
			db.MediaItems.AddRange(
				CreateMediaItem(1, MediaType.Image, "/legacy/null.jpg"),
				CreateMediaItem(2, MediaType.Image, "/legacy/empty.jpg"),
				CreateMediaItem(3, MediaType.Image, "/legacy/full.jpg"));
			await db.SaveChangesAsync();
		}

		// 旧 OwnsOne + OwnsMany + ToJson が出力していた生 JSON を SQL で直接設定する
		await this.SetRawJsonColumnsAsync(1, null, null);
		await this.SetRawJsonColumnsAsync(2, "{\"Entries\":[]}", "{}");
		const string legacyJson = """{"Entries":[{"Key":"Make","Value":"Canon"},{"Key":"Tag","Value":"A"},{"Key":"Tag","Value":"B"},{"Key":"日本語","Value":"値\"テスト"}]}""";
		await this.SetRawJsonColumnsAsync(3, legacyJson, null);

		await using var readDb = new MediaDeckDbContext(this._dbOptions);
		var trackedItems = await readDb.MediaItems.OrderBy(x => x.MediaItemId).ToListAsync();
		AssertLegacyItems(trackedItems);

		var noTrackingSplitItems = await readDb.MediaItems
			.AsNoTracking()
			.AsSplitQuery()
			.OrderBy(x => x.MediaItemId)
			.ToListAsync();
		AssertLegacyItems(noTrackingSplitItems);
	}

	/// <summary>
	/// 現行モデルでの新規保存・インプレース更新・差し替え・null 化・他カラム更新時の
	/// SQLite 生 JSON カラム形式が旧形式と完全一致することを検証します。
	/// </summary>
	[Fact]
	public async Task SaveAndUpdate_ProducesCompatibleJsonFormatAndPreservesUnmodifiedMetadata() {
		await using (var db = new MediaDeckDbContext(this._dbOptions)) {
			var nullItem = CreateMediaItem(1, MediaType.Image, "/save/null.jpg");
			nullItem.Metadata = null;

			var emptyItem = CreateMediaItem(2, MediaType.Image, "/save/empty.jpg");
			emptyItem.Metadata = new MediaMetadata();

			var fullItem = CreateMediaItem(3, MediaType.Pdf, "/save/full.pdf");
			fullItem.Metadata = new MediaMetadata {
				Entries = [
					new() { Key = "PageCount", Value = "12" },
					new() { Key = "Author", Value = "Test" }
				]
			};

			db.MediaItems.AddRange(nullItem, emptyItem, fullItem);
			await db.SaveChangesAsync();
		}

		(await this.ReadRawMetadataColumnAsync(1)).ShouldBeNull();
		(await this.ReadRawMetadataColumnAsync(2)).ShouldBe("{\"Entries\":[]}");
		(await this.ReadRawMetadataColumnAsync(3)).ShouldBe("""{"Entries":[{"Key":"PageCount","Value":"12"},{"Key":"Author","Value":"Test"}]}""");

		// インプレース更新（Entries.Add）およびオブジェクト差し替え
		await using (var db = new MediaDeckDbContext(this._dbOptions)) {
			var emptyItem = await db.MediaItems.SingleAsync(x => x.MediaItemId == 2);
			emptyItem.Metadata!.Entries.Add(new() {
				Key = "Added",
				Value = "Yes"
			});

			var fullItem = await db.MediaItems.SingleAsync(x => x.MediaItemId == 3);
			fullItem.Metadata = new MediaMetadata {
				Entries = [new() { Key = "PageCount", Value = "24" }]
			};

			await db.SaveChangesAsync();
		}

		(await this.ReadRawMetadataColumnAsync(2)).ShouldBe("""{"Entries":[{"Key":"Added","Value":"Yes"}]}""");
		(await this.ReadRawMetadataColumnAsync(3)).ShouldBe("""{"Entries":[{"Key":"PageCount","Value":"24"}]}""");

		// 別カラム（Rate）のみ更新した場合に Metadata が維持されること
		await using (var db = new MediaDeckDbContext(this._dbOptions)) {
			var fullItem = await db.MediaItems.SingleAsync(x => x.MediaItemId == 3);
			fullItem.Rate = 5;
			await db.SaveChangesAsync();
		}

		(await this.ReadRawMetadataColumnAsync(3)).ShouldBe("""{"Entries":[{"Key":"PageCount","Value":"24"}]}""");

		// Metadata を null へ変更した場合に NULL が書き込まれること
		await using (var db = new MediaDeckDbContext(this._dbOptions)) {
			var fullItem = await db.MediaItems.SingleAsync(x => x.MediaItemId == 3);
			fullItem.Metadata = null;
			await db.SaveChangesAsync();
		}

		(await this.ReadRawMetadataColumnAsync(3)).ShouldBeNull();
	}

	/// <summary>
	/// モデル初期化および読み書き時に EF Core 11 の <c>OwnedEntityMappedToJsonCollectionWarning</c>（EventId 20613）等の警告が出力されないことを検証します。
	/// </summary>
	[Fact]
	public async Task ModelCreationAndQueries_DoNotEmitOwnedEntityMappedToJsonCollectionWarning() {
		await using (var db = new MediaDeckDbContext(this._dbOptions)) {
			var item = CreateMediaItem(1, MediaType.Image, "/warn/test.jpg");
			item.Metadata = new MediaMetadata {
				Entries = [new() { Key = "Make", Value = "Nikon" }]
			};
			db.MediaItems.Add(item);
			await db.SaveChangesAsync();
		}

		await using (var readDb = new MediaDeckDbContext(this._dbOptions)) {
			var loaded = await readDb.MediaItems.AsNoTracking().SingleAsync(x => x.MediaItemId == 1);
			loaded.Metadata.ShouldNotBeNull();
			loaded.Metadata.Entries.Count.ShouldBe(1);
		}

		this._warningLogs.ShouldNotContain(x =>
			x.EventId == 20613
			|| x.EventName.Contains("OwnedEntityMappedToJsonCollectionWarning", StringComparison.Ordinal)
			|| x.Message.Contains("obsolete", StringComparison.OrdinalIgnoreCase));
		this._warningLogs.ShouldBeEmpty();
	}

	/// <summary>
	/// <see cref="ArchiveMediaItemTypeProvider"/>・<see cref="ImageMediaItemTypeProvider"/>・<see cref="PdfMediaItemTypeProvider"/> の
	/// <c>IncludeTables</c> 経由でクエリを実行しても例外が発生せず、<see cref="MediaItem.Metadata"/> が読み込まれることを検証します。
	/// </summary>
	[Fact]
	public async Task IncludeTables_OnProviders_LoadsMetadataWithoutException() {
		await using (var db = new MediaDeckDbContext(this._dbOptions)) {
			var archiveItem = CreateMediaItem(1, MediaType.Archive, "/types/a.zip");
			archiveItem.Metadata = new MediaMetadata { Entries = [new() { Key = "PageCount", Value = "10" }] };

			var imageItem = CreateMediaItem(2, MediaType.Image, "/types/b.jpg");
			imageItem.Metadata = new MediaMetadata { Entries = [new() { Key = "Exif/Make", Value = "Sony" }] };
			imageItem.ImageFile = new ImageFile { MediaItemId = 2, Orientation = 1 };

			var pdfItem = CreateMediaItem(3, MediaType.Pdf, "/types/c.pdf");
			pdfItem.Metadata = new MediaMetadata { Entries = [new() { Key = "PageCount", Value = "5" }] };

			db.MediaItems.AddRange(archiveItem, imageItem, pdfItem);
			await db.SaveChangesAsync();
		}

		var services = new ServiceCollection();
		MediaDeck.Composition.DIRegistration.AddGeneratedServices(services);
		services.AddSingleton<IStringProvider, StubStringProvider>();
		await using var sp = services.BuildServiceProvider();
		var config = sp.GetRequiredService<MediaDeck.Composition.Stores.Config.Model.ConfigModel>();

		var archiveProvider = new ArchiveMediaItemTypeProvider(config);
		var imageProvider = new ImageMediaItemTypeProvider(config);
		var pdfProvider = new PdfMediaItemTypeProvider(config);

		await using var readDb = new MediaDeckDbContext(this._dbOptions);

		var loadedArchive = await archiveProvider.IncludeTables(readDb.MediaItems)
			.SingleAsync(x => x.MediaItemId == 1);
		loadedArchive.Metadata.ShouldNotBeNull();
		loadedArchive.Metadata.Entries.ShouldContain(e => e.Key == "PageCount" && e.Value == "10");

		var loadedImage = await imageProvider.IncludeTables(readDb.MediaItems)
			.SingleAsync(x => x.MediaItemId == 2);
		loadedImage.ImageFile.ShouldNotBeNull();
		loadedImage.ImageFile.Orientation.ShouldBe(1);
		loadedImage.Metadata.ShouldNotBeNull();
		loadedImage.Metadata.Entries.ShouldContain(e => e.Key == "Exif/Make" && e.Value == "Sony");

		var loadedPdf = await pdfProvider.IncludeTables(readDb.MediaItems)
			.SingleAsync(x => x.MediaItemId == 3);
		loadedPdf.Metadata.ShouldNotBeNull();
		loadedPdf.Metadata.Entries.ShouldContain(e => e.Key == "PageCount" && e.Value == "5");
	}

	/// <summary>
	/// 旧形式データから復元された 3 件の <see cref="MediaItem"/> を検証します。
	/// </summary>
	/// <param name="items">検証対象のアイテム一覧</param>
	private static void AssertLegacyItems(List<MediaItem> items) {
		items.Count.ShouldBe(3);

		items[0].Metadata.ShouldBeNull();
		items[0].AdditionalInfo.ShouldBeNull();

		items[1].Metadata.ShouldNotBeNull();
		items[1].Metadata!.Entries.ShouldBeEmpty();
		items[1].AdditionalInfo.ShouldNotBeNull();

		items[2].Metadata.ShouldNotBeNull();
		items[2].Metadata!.Entries.Count.ShouldBe(4);
		items[2].Metadata!.Entries[0].Key.ShouldBe("Make");
		items[2].Metadata!.Entries[0].Value.ShouldBe("Canon");
		items[2].Metadata!.Entries[1].Key.ShouldBe("Tag");
		items[2].Metadata!.Entries[1].Value.ShouldBe("A");
		items[2].Metadata!.Entries[2].Key.ShouldBe("Tag");
		items[2].Metadata!.Entries[2].Value.ShouldBe("B");
		items[2].Metadata!.Entries[3].Key.ShouldBe("日本語");
		items[2].Metadata!.Entries[3].Value.ShouldBe("値\"テスト");
	}

	/// <summary>
	/// テスト用の最小構成 <see cref="MediaItem"/> を生成します。
	/// </summary>
	/// <param name="id">メディアアイテム ID</param>
	/// <param name="mediaType">メディア種別</param>
	/// <param name="filePath">ファイルパス</param>
	/// <returns>生成したエンティティ</returns>
	private static MediaItem CreateMediaItem(long id, MediaType mediaType, string filePath) {
		return new MediaItem {
			MediaItemId = id,
			MediaType = mediaType,
			DirectoryPath = "/dir",
			FilePath = filePath,
			Description = string.Empty,
			IsUnderFolderGroup = false
		};
	}

	/// <summary>
	/// 指定行の <c>Metadata</c>・<c>AdditionalInfo</c> カラムへ生 JSON 文字列を直接書き込みます。
	/// </summary>
	/// <param name="mediaItemId">更新対象の ID</param>
	/// <param name="metadataJson">書き込む Metadata JSON（null 可）</param>
	/// <param name="additionalInfoJson">書き込む AdditionalInfo JSON（null 可）</param>
	private async Task SetRawJsonColumnsAsync(long mediaItemId, string? metadataJson, string? additionalInfoJson) {
		await using var cmd = this._connection.CreateCommand();
		cmd.CommandText = "UPDATE MediaItems SET Metadata = @metadata, AdditionalInfo = @additionalInfo WHERE MediaItemId = @id";
		cmd.Parameters.AddWithValue("@id", mediaItemId);
		cmd.Parameters.AddWithValue("@metadata", (object?)metadataJson ?? DBNull.Value);
		cmd.Parameters.AddWithValue("@additionalInfo", (object?)additionalInfoJson ?? DBNull.Value);
		await cmd.ExecuteNonQueryAsync();
	}

	/// <summary>
	/// 指定行の <c>Metadata</c> カラムの生文字列を SQL で直接読み取ります。
	/// </summary>
	/// <param name="mediaItemId">取得対象の ID</param>
	/// <returns>生の JSON 文字列（SQL NULL の場合は null）</returns>
	private async Task<string?> ReadRawMetadataColumnAsync(long mediaItemId) {
		await using var cmd = this._connection.CreateCommand();
		cmd.CommandText = "SELECT Metadata FROM MediaItems WHERE MediaItemId = @id";
		cmd.Parameters.AddWithValue("@id", mediaItemId);
		var result = await cmd.ExecuteScalarAsync();
		return result is null or DBNull ? null : (string)result;
	}

	/// <summary>
	/// 捕捉したログエントリ。
	/// </summary>
	private sealed record LogEntry(LogLevel Level, int EventId, string EventName, string Message);

	/// <summary>
	/// Warning 以上のログを捕捉するテスト用 <see cref="ILoggerProvider"/>。
	/// </summary>
	private sealed class CaptureWarningLoggerProvider(List<LogEntry> logs) : ILoggerProvider {
		/// <inheritdoc />
		public ILogger CreateLogger(string categoryName) {
			return new CaptureWarningLogger(logs);
		}

		/// <inheritdoc />
		public void Dispose() {
		}

		/// <summary>
		/// Warning 以上のログを記録するロガー実装。
		/// </summary>
		private sealed class CaptureWarningLogger(List<LogEntry> logs) : ILogger {
			/// <inheritdoc />
			public IDisposable? BeginScope<TState>(TState state) where TState : notnull {
				return null;
			}

			/// <inheritdoc />
			public bool IsEnabled(LogLevel logLevel) {
				return logLevel >= LogLevel.Warning;
			}

			/// <inheritdoc />
			public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) {
				if (this.IsEnabled(logLevel)) {
					logs.Add(new LogEntry(logLevel, eventId.Id, eventId.Name ?? string.Empty, formatter(state, exception)));
				}
			}
		}
	}
}