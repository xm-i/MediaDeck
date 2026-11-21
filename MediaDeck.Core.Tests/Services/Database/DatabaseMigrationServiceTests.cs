using System.Data.Common;
using MediaDeck.Composition.Database;
using MediaDeck.Composition.Database.Migrations;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Tables;
using MediaDeck.Core.Services.Database;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;

namespace MediaDeck.Core.Tests.Services.Database;

/// <summary>
/// <see cref="DatabaseMigrationService"/> およびシステム共通インデックスの適用を検証するテストクラス。
/// </summary>
public sealed class DatabaseMigrationServiceTests : IDisposable {
	private readonly string _tempDir;
	private readonly SqliteConnection _connection;
	private readonly DbContextOptions<MediaDeckDbContext> _options;
	private readonly List<CapturedLogEntry> _logs = [];
	private readonly LoggerFactory _loggerFactory;

	/// <summary>
	/// テスト用のインメモリ SQLite 接続、一時ディレクトリ、ロガーを初期化します。
	/// </summary>
	public DatabaseMigrationServiceTests() {
		this._tempDir = Path.Combine(Path.GetTempPath(), $"MediaDeck_MigrationTests_{Guid.NewGuid():N}");
		Directory.CreateDirectory(this._tempDir);

		this._connection = new SqliteConnection("DataSource=:memory:");
		this._connection.Open();

		this._options = new DbContextOptionsBuilder<MediaDeckDbContext>()
			.UseSqlite(this._connection)
			.Options;

		this._loggerFactory = new LoggerFactory([new CaptureLoggerProvider(this._logs)]);
	}

	/// <summary>
	/// テストで使用した接続・ロガー・一時ディレクトリを解放します。
	/// </summary>
	public void Dispose() {
		this._loggerFactory.Dispose();
		this._connection.Dispose();
		SqliteConnection.ClearAllPools();
		if (Directory.Exists(this._tempDir)) {
			Directory.Delete(this._tempDir, true);
		}
	}

	/// <summary>
	/// 存在しないファイルパスを指定して <see cref="DatabaseMigrationService.InitializeAsync"/> を実行すると、
	/// DB ファイルが新規作成され、最新バージョンおよびシステム共通インデックスが構築されることを検証します。
	/// </summary>
	[Fact]
	public async Task InitializeAsync_CreatesNewDatabaseWithLatestVersionAndIndexes() {
		var dbPath = Path.Combine(this._tempDir, "nested", "mediadeck.db");
		var fileOptions = CreateFileDbOptions(dbPath);
		using var service = this.CreateService(CreateDbFactoryMock(fileOptions).Object);

		await service.InitializeAsync(dbPath);

		File.Exists(dbPath).ShouldBeTrue();
		await using var db = new MediaDeckDbContext(fileOptions);
		var version = await db.DbVersions.SingleAsync();
		version.Id.ShouldBe(DatabaseMigrationRegistry.DbVersionRecordId);
		version.Version.ShouldBe(DatabaseMigrationRegistry.LatestVersion);

		var connection = db.Database.GetDbConnection();
		await connection.OpenAsync();
		(await GetIndexSqlAsync(connection, "IX_MediaItems_DirectoryPath")).ShouldNotBeNullOrWhiteSpace();
		(await GetIndexSqlAsync(connection, "IX_MediaItems_PreHash_IsExists")).ShouldNotBeNullOrWhiteSpace();
	}

	/// <summary>
	/// 事前に 0 バイトの空ファイルが存在する状態でも <see cref="DatabaseMigrationService.InitializeAsync"/> が
	/// スキーマ作成および最新バージョン登録を完了できることを検証します。
	/// </summary>
	[Fact]
	public async Task InitializeAsync_WithEmptyFile_InitializesSchemaAndLatestVersion() {
		var dbPath = Path.Combine(this._tempDir, "empty.db");
		await using (File.Create(dbPath)) {
		}
		var fileOptions = CreateFileDbOptions(dbPath);
		using var service = this.CreateService(CreateDbFactoryMock(fileOptions).Object);

		await service.InitializeAsync(dbPath);

		await using var db = new MediaDeckDbContext(fileOptions);
		var version = await db.DbVersions.SingleAsync();
		version.Id.ShouldBe(DatabaseMigrationRegistry.DbVersionRecordId);
		version.Version.ShouldBe(DatabaseMigrationRegistry.LatestVersion);

		var connection = db.Database.GetDbConnection();
		await connection.OpenAsync();
		(await GetIndexSqlAsync(connection, "IX_MediaItems_DirectoryPath")).ShouldNotBeNullOrWhiteSpace();
		(await GetIndexSqlAsync(connection, "IX_MediaItems_PreHash_IsExists")).ShouldNotBeNullOrWhiteSpace();
	}

	/// <summary>
	/// 既存のバージョン 1 データベースをバージョン 2 へマイグレーションした際、既存データが保持され、
	/// 生成されたインデックス DDL が <see cref="DbContext.Database"/> の <c>EnsureCreated</c> と完全一致することを検証します。
	/// </summary>
	[Fact]
	public async Task MigrateAsync_FromVersion1_AddsIndexesMatchingEnsureCreatedDdlAndPreservesData() {
		using var referenceConnection = new SqliteConnection("DataSource=:memory:");
		await referenceConnection.OpenAsync();
		var referenceOptions = new DbContextOptionsBuilder<MediaDeckDbContext>().UseSqlite(referenceConnection).Options;
		await using (var refDb = new MediaDeckDbContext(referenceOptions)) {
			await refDb.Database.EnsureCreatedAsync();
		}
		var expectedDirIndexSql = await GetIndexSqlAsync(referenceConnection, "IX_MediaItems_DirectoryPath");
		var expectedPreHashIndexSql = await GetIndexSqlAsync(referenceConnection, "IX_MediaItems_PreHash_IsExists");

		await using (var setupDb = new MediaDeckDbContext(this._options)) {
			await setupDb.Database.EnsureCreatedAsync();
			setupDb.DbVersions.Add(new DbVersion { Id = DatabaseMigrationRegistry.DbVersionRecordId, Version = 1 });
			setupDb.MediaItems.Add(CreateMediaItem(1, @"C:\Media\FolderA", @"C:\Media\FolderA\sample.jpg", "pre-1"));
			await setupDb.SaveChangesAsync();

			await setupDb.Database.ExecuteSqlRawAsync("""DROP INDEX "IX_MediaItems_DirectoryPath";""");
			await setupDb.Database.ExecuteSqlRawAsync("""DROP INDEX "IX_MediaItems_PreHash_IsExists";""");
		}

		using var service = this.CreateService(CreateDbFactoryMock(this._options).Object);
		await service.MigrateAsync();

		await using (var verifyDb = new MediaDeckDbContext(this._options)) {
			var version = await verifyDb.DbVersions.SingleAsync();
			version.Version.ShouldBe(2);

			var item = await verifyDb.MediaItems.SingleAsync();
			item.FilePath.ShouldBe(@"C:\Media\FolderA\sample.jpg");
			item.PreHash.ShouldBe("pre-1");
		}

		(await GetIndexSqlAsync(this._connection, "IX_MediaItems_DirectoryPath")).ShouldBe(expectedDirIndexSql);
		(await GetIndexSqlAsync(this._connection, "IX_MediaItems_PreHash_IsExists")).ShouldBe(expectedPreHashIndexSql);
	}

	/// <summary>
	/// <c>DbVersions</c> テーブルのレコード（<c>Id = 1</c>）が欠落している既存データベースでも
	/// マイグレーションが適用され、バージョンレコードが登録されることを検証します。
	/// </summary>
	[Fact]
	public async Task MigrateAsync_WithEmptyDbVersionsTable_AppliesMigrationsAndInsertsVersionRecord() {
		await using (var setupDb = new MediaDeckDbContext(this._options)) {
			await setupDb.Database.EnsureCreatedAsync();
			await setupDb.Database.ExecuteSqlRawAsync("""DROP INDEX "IX_MediaItems_DirectoryPath";""");
			await setupDb.Database.ExecuteSqlRawAsync("""DROP INDEX "IX_MediaItems_PreHash_IsExists";""");
		}

		using var service = this.CreateService(CreateDbFactoryMock(this._options).Object);
		await service.MigrateAsync();

		await using var verifyDb = new MediaDeckDbContext(this._options);
		var version = await verifyDb.DbVersions.SingleAsync();
		version.Id.ShouldBe(DatabaseMigrationRegistry.DbVersionRecordId);
		version.Version.ShouldBe(DatabaseMigrationRegistry.LatestVersion);

		(await GetIndexSqlAsync(this._connection, "IX_MediaItems_DirectoryPath")).ShouldNotBeNullOrWhiteSpace();
		(await GetIndexSqlAsync(this._connection, "IX_MediaItems_PreHash_IsExists")).ShouldNotBeNullOrWhiteSpace();
	}

	/// <summary>
	/// すでに最新バージョン済みの場合はステップを再実行せず、またインデックスが既に存在する v1 DB でも冪等に v2 へ更新できることを検証します。
	/// </summary>
	[Fact]
	public async Task MigrateAsync_IsIdempotentAcrossReExecutionAndExistingIndexes() {
		await using (var setupDb = new MediaDeckDbContext(this._options)) {
			await setupDb.Database.EnsureCreatedAsync();
			setupDb.DbVersions.Add(new DbVersion { Id = DatabaseMigrationRegistry.DbVersionRecordId, Version = 1 });
			await setupDb.SaveChangesAsync();
		}

		using var realService = this.CreateService(CreateDbFactoryMock(this._options).Object);
		// インデックスが既に存在する v1 DB でも IF NOT EXISTS で成功すること
		await realService.MigrateAsync();
		// 最新状態で再実行しても正常終了すること
		await realService.MigrateAsync();

		var applyCount = 0;
		var trackingMigration = new DelegateMigration(
			2,
			"Tracking v2",
			(_, _) => {
				applyCount++;
				return Task.CompletedTask;
			});

		using var customService = new DatabaseMigrationService(
			CreateDbFactoryMock(this._options).Object,
			this._loggerFactory.CreateLogger<DatabaseMigrationService>(),
			[trackingMigration],
			2);
		await customService.MigrateAsync();

		applyCount.ShouldBe(0);
		await using var verifyDb = new MediaDeckDbContext(this._options);
		(await verifyDb.DbVersions.SingleAsync()).Version.ShouldBe(2);
	}

	/// <summary>
	/// DB バージョンがアプリ側の最新バージョンより大きい場合はマイグレーションをスキップし、警告ログを出力することを検証します。
	/// </summary>
	[Fact]
	public async Task MigrateAsync_WhenDatabaseVersionIsNewer_SkipsMigrationAndLogsWarning() {
		await using (var setupDb = new MediaDeckDbContext(this._options)) {
			await setupDb.Database.EnsureCreatedAsync();
			setupDb.DbVersions.Add(new DbVersion { Id = DatabaseMigrationRegistry.DbVersionRecordId, Version = 99 });
			await setupDb.SaveChangesAsync();
		}

		var executed = false;
		var step = new DelegateMigration(
			2,
			"Should not run",
			(_, _) => {
				executed = true;
				return Task.CompletedTask;
			});

		using var service = new DatabaseMigrationService(
			CreateDbFactoryMock(this._options).Object,
			this._loggerFactory.CreateLogger<DatabaseMigrationService>(),
			[step],
			2);
		await service.MigrateAsync();

		executed.ShouldBeFalse();
		this._logs.ShouldContain(x => x.Level == LogLevel.Warning);

		await using var verifyDb = new MediaDeckDbContext(this._options);
		(await verifyDb.DbVersions.SingleAsync()).Version.ShouldBe(99);
	}

	/// <summary>
	/// 複数ステップがバージョン昇順で実行されることを検証します。
	/// </summary>
	[Fact]
	public async Task MigrateAsync_ExecutesMultipleMigrationsInAscendingOrder() {
		await using (var setupDb = new MediaDeckDbContext(this._options)) {
			await setupDb.Database.EnsureCreatedAsync();
			setupDb.DbVersions.Add(new DbVersion { Id = DatabaseMigrationRegistry.DbVersionRecordId, Version = 1 });
			await setupDb.SaveChangesAsync();
		}

		var executionOrder = new List<int>();
		var v3 = new DelegateMigration(
			3,
			"Step 3",
			(_, _) => {
				executionOrder.Add(3);
				return Task.CompletedTask;
			});
		var v2 = new DelegateMigration(
			2,
			"Step 2",
			(_, _) => {
				executionOrder.Add(2);
				return Task.CompletedTask;
			});

		using var service = new DatabaseMigrationService(
			CreateDbFactoryMock(this._options).Object,
			this._loggerFactory.CreateLogger<DatabaseMigrationService>(),
			[v3, v2],
			3);
		await service.MigrateAsync();

		executionOrder.ShouldBe([2, 3]);
		await using var verifyDb = new MediaDeckDbContext(this._options);
		(await verifyDb.DbVersions.SingleAsync()).Version.ShouldBe(3);
	}

	/// <summary>
	/// 途中のマイグレーションステップで例外が発生した場合、成功済みステップのバージョンは維持され、
	/// 失敗したステップの DDL 変更およびバージョン更新はトランザクションによりロールバックされることを検証します。
	/// </summary>
	[Fact]
	public async Task MigrateAsync_WhenStepFails_RollsBackFailedStepAndKeepsPreviousStep() {
		await using (var setupDb = new MediaDeckDbContext(this._options)) {
			await setupDb.Database.EnsureCreatedAsync();
			setupDb.DbVersions.Add(new DbVersion { Id = DatabaseMigrationRegistry.DbVersionRecordId, Version = 1 });
			await setupDb.SaveChangesAsync();
		}

		var v2 = new DelegateMigration(
			2,
			"Create table v2",
			async (db, ct) => {
				await db.Database.ExecuteSqlRawAsync("""CREATE TABLE "TestMigrationV2" ("Id" INTEGER PRIMARY KEY);""", ct);
			});
		var v3 = new DelegateMigration(
			3,
			"Create table v3 then fail",
			async (db, ct) => {
				await db.Database.ExecuteSqlRawAsync("""CREATE TABLE "TestMigrationV3" ("Id" INTEGER PRIMARY KEY);""", ct);
				throw new InvalidOperationException("Simulated migration failure.");
			});

		using var service = new DatabaseMigrationService(
			CreateDbFactoryMock(this._options).Object,
			this._loggerFactory.CreateLogger<DatabaseMigrationService>(),
			[v2, v3],
			3);

		await Should.ThrowAsync<InvalidOperationException>(() => service.MigrateAsync());

		(await GetTableSqlAsync(this._connection, "TestMigrationV2")).ShouldNotBeNullOrWhiteSpace();
		(await GetTableSqlAsync(this._connection, "TestMigrationV3")).ShouldBeNull();

		await using var verifyDb = new MediaDeckDbContext(this._options);
		(await verifyDb.DbVersions.SingleAsync()).Version.ShouldBe(2);
	}

	/// <summary>
	/// コンストラクタのマイグレーション定義バリデーションが不正な設定を正しく拒否することを検証します。
	/// </summary>
	[Fact]
	public void Constructor_WithInvalidMigrationDefinitions_ThrowsExpectedException() {
		var factory = CreateDbFactoryMock(this._options).Object;
		var logger = this._loggerFactory.CreateLogger<DatabaseMigrationService>();

		Should.Throw<ArgumentOutOfRangeException>(() =>
			new DatabaseMigrationService(factory, logger, [], 0));

		Should.Throw<ArgumentException>(() =>
			new DatabaseMigrationService(factory, logger, [null!], 1));

		Should.Throw<ArgumentException>(() =>
			new DatabaseMigrationService(factory, logger, [new DelegateMigration(1, "Invalid", (_, _) => Task.CompletedTask)], 2));

		Should.Throw<ArgumentException>(() =>
			new DatabaseMigrationService(factory, logger, [new DelegateMigration(3, "Exceeds", (_, _) => Task.CompletedTask)], 2));

		Should.Throw<ArgumentException>(() =>
			new DatabaseMigrationService(
				factory,
				logger,
				[
					new DelegateMigration(2, "Dup1", (_, _) => Task.CompletedTask),
					new DelegateMigration(2, "Dup2", (_, _) => Task.CompletedTask)
				],
				2));

		Should.Throw<ArgumentException>(() =>
			new DatabaseMigrationService(factory, logger, [new DelegateMigration(2, "Only v2", (_, _) => Task.CompletedTask)], 3));
	}

	/// <summary>
	/// 実際の EF Core LINQ クエリに対して SQLite の <c>EXPLAIN QUERY PLAN</c> が
	/// <c>IX_MediaItems_DirectoryPath</c> および <c>IX_MediaItems_PreHash_IsExists</c> をカバリングインデックスとして選択することを検証します。
	/// </summary>
	[Fact]
	public async Task ExplainQueryPlan_UsesCoveringIndexesForSystemQueries() {
		await using (var setupDb = new MediaDeckDbContext(this._options)) {
			await setupDb.Database.EnsureCreatedAsync();

			for (var i = 1; i <= 30; i++) {
				var dir = $@"C:\Media\Dir{i % 5}";
				var preHash = i <= 10 ? $"dup-{i % 3}" : $"uniq-{i}";
				var item = CreateMediaItem(i, dir, Path.Combine(dir, $"file_{i}.jpg"), preHash);
				item.IsExists = i % 7 != 0;
				setupDb.MediaItems.Add(item);
			}
			await setupDb.SaveChangesAsync();
		}

		await using var db = new MediaDeckDbContext(this._options);
		await db.Database.OpenConnectionAsync();

		// 1. FolderRepository.Load 相当のクエリ
		var folderGroupQuery = db.MediaItems
			.GroupBy(x => x.DirectoryPath)
			.Select(x => new { x.Key, Count = x.Count() });
		var folderPlan = await ExplainQueryPlanAsync(this._connection, folderGroupQuery);
		folderPlan.ShouldContain("COVERING INDEX IX_MediaItems_DirectoryPath");

		// 2. FileHashUpdatorService.CreateDuplicatePreHashesQuery 相当のクエリ
		var duplicatePreHashesQuery = db.MediaItems
			.Where(m => m.IsExists && m.PreHash != null)
			.GroupBy(m => m.PreHash)
			.Where(g => g.Count() >= 2)
			.Select(g => g.Key);
		var hashServicePlan = await ExplainQueryPlanAsync(this._connection, duplicatePreHashesQuery);
		hashServicePlan.ShouldContain("COVERING INDEX IX_MediaItems_PreHash_IsExists");

		// 3. DuplicateFileDetectorService (PreHash 重複検出) 相当のクエリ
		var duplicateDetectorQuery = db.MediaItems
			.Where(x => x.PreHash != null)
			.GroupBy(x => x.PreHash!)
			.Where(g => g.Count() > 1)
			.Select(g => g.Key);
		var detectorPlan = await ExplainQueryPlanAsync(this._connection, duplicateDetectorQuery);
		detectorPlan.ShouldContain("COVERING INDEX IX_MediaItems_PreHash_IsExists");
	}

	/// <summary>
	/// テスト用の <see cref="DatabaseMigrationService"/> を生成します。
	/// </summary>
	private DatabaseMigrationService CreateService(IDbContextFactory<MediaDeckDbContext> dbFactory) {
		return new DatabaseMigrationService(dbFactory, this._loggerFactory.CreateLogger<DatabaseMigrationService>());
	}

	/// <summary>
	/// ファイルロックを防ぐためプーリング無効の SQLite <see cref="DbContextOptions{MediaDeckDbContext}"/> を生成します。
	/// </summary>
	private static DbContextOptions<MediaDeckDbContext> CreateFileDbOptions(string dbPath) {
		var sb = new SqliteConnectionStringBuilder {
			DataSource = dbPath,
			Pooling = false
		};
		return new DbContextOptionsBuilder<MediaDeckDbContext>()
			.UseSqlite(sb.ConnectionString)
			.Options;
	}

	/// <summary>
	/// 指定した <see cref="DbContextOptions{MediaDeckDbContext}"/> で <see cref="MediaDeckDbContext"/> を生成するモックファクトリを作成します。
	/// </summary>
	private static Mock<IDbContextFactory<MediaDeckDbContext>> CreateDbFactoryMock(DbContextOptions<MediaDeckDbContext> options) {
		var mock = new Mock<IDbContextFactory<MediaDeckDbContext>>();
		mock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
			.Returns(() => Task.FromResult(new MediaDeckDbContext(options)));
		return mock;
	}

	/// <summary>
	/// テスト用の <see cref="MediaItem"/> を生成します。
	/// </summary>
	private static MediaItem CreateMediaItem(long id, string directoryPath, string filePath, string? preHash = null) {
		return new MediaItem {
			MediaItemId = id,
			MediaType = MediaType.Image,
			DirectoryPath = directoryPath,
			FilePath = filePath,
			Description = string.Empty,
			IsExists = true,
			IsUnderFolderGroup = false,
			PreHash = preHash
		};
	}

	/// <summary>
	/// <c>sqlite_master</c> から指定インデックスの定義 SQL を取得します。
	/// </summary>
	private static Task<string?> GetIndexSqlAsync(DbConnection connection, string indexName) {
		return GetMasterSqlAsync(connection, "index", indexName);
	}

	/// <summary>
	/// <c>sqlite_master</c> から指定テーブルの定義 SQL を取得します。
	/// </summary>
	private static Task<string?> GetTableSqlAsync(DbConnection connection, string tableName) {
		return GetMasterSqlAsync(connection, "table", tableName);
	}

	/// <summary>
	/// <c>sqlite_master</c> から指定種別・名前のオブジェクト定義 SQL を取得します。
	/// </summary>
	private static async Task<string?> GetMasterSqlAsync(DbConnection connection, string type, string name) {
		await using var cmd = connection.CreateCommand();
		cmd.CommandText = "SELECT sql FROM sqlite_master WHERE type = @type AND name = @name";

		var typeParam = cmd.CreateParameter();
		typeParam.ParameterName = "@type";
		typeParam.Value = type;
		cmd.Parameters.Add(typeParam);

		var nameParam = cmd.CreateParameter();
		nameParam.ParameterName = "@name";
		nameParam.Value = name;
		cmd.Parameters.Add(nameParam);

		var result = await cmd.ExecuteScalarAsync();
		return result is null or DBNull ? null : (string)result;
	}

	/// <summary>
	/// EF Core の <see cref="IQueryable"/> から実際の SQL とパラメータを取得し、<c>EXPLAIN QUERY PLAN</c> の結果を返します。
	/// </summary>
	private static async Task<string> ExplainQueryPlanAsync(SqliteConnection connection, IQueryable query) {
		await using var sourceCmd = query.CreateDbCommand();
		await using var explainCmd = connection.CreateCommand();
		explainCmd.CommandText = $"EXPLAIN QUERY PLAN {sourceCmd.CommandText}";
		foreach (DbParameter sourceParam in sourceCmd.Parameters) {
			var copy = explainCmd.CreateParameter();
			copy.ParameterName = sourceParam.ParameterName;
			copy.Value = sourceParam.Value ?? DBNull.Value;
			explainCmd.Parameters.Add(copy);
		}

		var lines = new List<string>();
		await using var reader = await explainCmd.ExecuteReaderAsync();
		while (await reader.ReadAsync()) {
			lines.Add(reader.GetString(3));
		}
		return string.Join(Environment.NewLine, lines);
	}

	/// <summary>
	/// テスト用の委譲マイグレーション実装。
	/// </summary>
	private sealed class DelegateMigration(
		int targetVersion,
		string description,
		Func<MediaDeckDbContext, CancellationToken, Task> apply) : IDatabaseMigration {
		/// <inheritdoc />
		public int TargetVersion {
			get {
				return targetVersion;
			}
		}

		/// <inheritdoc />
		public string Description {
			get {
				return description;
			}
		}

		/// <inheritdoc />
		public Task ApplyAsync(MediaDeckDbContext dbContext, CancellationToken cancellationToken = default) {
			return apply(dbContext, cancellationToken);
		}
	}

	/// <summary>
	/// 捕捉したログエントリ。
	/// </summary>
	private sealed record CapturedLogEntry(LogLevel Level, string Message);

	/// <summary>
	/// すべてのログを捕捉するテスト用 <see cref="ILoggerProvider"/>。
	/// </summary>
	private sealed class CaptureLoggerProvider(List<CapturedLogEntry> logs) : ILoggerProvider {
		/// <inheritdoc />
		public ILogger CreateLogger(string categoryName) {
			return new CaptureLogger(logs);
		}

		/// <inheritdoc />
		public void Dispose() {
		}

		/// <summary>
		/// テスト用ロガー実装。
		/// </summary>
		private sealed class CaptureLogger(List<CapturedLogEntry> logs) : ILogger {
			/// <inheritdoc />
			public IDisposable? BeginScope<TState>(TState state) where TState : notnull {
				return null;
			}

			/// <inheritdoc />
			public bool IsEnabled(LogLevel logLevel) {
				return true;
			}

			/// <inheritdoc />
			public void Log<TState>(
				LogLevel logLevel,
				EventId eventId,
				TState state,
				Exception? exception,
				Func<TState, Exception?, string> formatter) {
				logs.Add(new CapturedLogEntry(logLevel, formatter(state, exception)));
			}
		}
	}
}