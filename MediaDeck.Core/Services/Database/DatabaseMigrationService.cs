using MediaDeck.Common.Base;
using MediaDeck.Composition.Database;
using MediaDeck.Composition.Database.Migrations;
using MediaDeck.Composition.Tables;
using Microsoft.Extensions.Logging;

namespace MediaDeck.Core.Services.Database;

/// <summary>
/// データベースの初期化およびバージョン管理されたマイグレーションを実行するサービスです。
/// </summary>
[Inject(InjectServiceLifetime.Singleton, typeof(IDatabaseMigrationService))]
public class DatabaseMigrationService : ServiceBase, IDatabaseMigrationService {
	private readonly IDbContextFactory<MediaDeckDbContext> _dbFactory;
	private readonly ILogger<DatabaseMigrationService> _logger;
	private readonly IDatabaseMigration[] _migrations;
	private readonly int _latestVersion;

	/// <summary>
	/// <see cref="DatabaseMigrationService"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="dbFactory">データベースコンテキストファクトリ。</param>
	/// <param name="logger">ロガー。</param>
	public DatabaseMigrationService(
		IDbContextFactory<MediaDeckDbContext> dbFactory,
		ILogger<DatabaseMigrationService> logger)
		: this(dbFactory, logger, DatabaseMigrationRegistry.Migrations, DatabaseMigrationRegistry.LatestVersion) {
	}

	/// <summary>
	/// テスト用にマイグレーション定義と最新バージョンを指定して <see cref="DatabaseMigrationService"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="dbFactory">データベースコンテキストファクトリ。</param>
	/// <param name="logger">ロガー。</param>
	/// <param name="migrations">適用可能なマイグレーションステップ一覧。</param>
	/// <param name="latestVersion">対象となる最新データベースバージョン。</param>
	internal DatabaseMigrationService(
		IDbContextFactory<MediaDeckDbContext> dbFactory,
		ILogger<DatabaseMigrationService> logger,
		IReadOnlyList<IDatabaseMigration> migrations,
		int latestVersion) {
		ArgumentNullException.ThrowIfNull(dbFactory);
		ArgumentNullException.ThrowIfNull(logger);
		ArgumentNullException.ThrowIfNull(migrations);
		if (latestVersion < DatabaseMigrationRegistry.InitialVersion) {
			throw new ArgumentOutOfRangeException(
				nameof(latestVersion),
				latestVersion,
				$"Latest version must be at least {DatabaseMigrationRegistry.InitialVersion}.");
		}

		var seenVersions = new HashSet<int>();
		var validated = new List<IDatabaseMigration>(migrations.Count);
		foreach (var migration in migrations) {
			if (migration == null) {
				throw new ArgumentException("Migration entry must not be null.", nameof(migrations));
			}
			if (migration.TargetVersion <= DatabaseMigrationRegistry.InitialVersion || migration.TargetVersion > latestVersion) {
				throw new ArgumentException(
					$"Migration target version {migration.TargetVersion} must be between {DatabaseMigrationRegistry.InitialVersion + 1} and {latestVersion}.",
					nameof(migrations));
			}
			if (!seenVersions.Add(migration.TargetVersion)) {
				throw new ArgumentException(
					$"Duplicate migration target version: {migration.TargetVersion}.",
					nameof(migrations));
			}
			validated.Add(migration);
		}

		if (latestVersion > DatabaseMigrationRegistry.InitialVersion && !seenVersions.Contains(latestVersion)) {
			throw new ArgumentException(
				$"No migration step found for latest version {latestVersion}.",
				nameof(migrations));
		}

		this._dbFactory = dbFactory;
		this._logger = logger;
		this._migrations = [.. validated.OrderBy(static m => m.TargetVersion)];
		this._latestVersion = latestVersion;
	}

	/// <inheritdoc />
	public async Task InitializeAsync(string databaseFilePath, CancellationToken cancellationToken = default) {
		ArgumentException.ThrowIfNullOrWhiteSpace(databaseFilePath);

		if (!File.Exists(databaseFilePath)) {
			var directoryPath = Path.GetDirectoryName(databaseFilePath);
			if (!string.IsNullOrEmpty(directoryPath)) {
				Directory.CreateDirectory(directoryPath);
			}
			// 事前に0バイトファイルを作らないと EnsureCreatedAsync で失敗する環境があるため作成する
			using var _ = File.Create(databaseFilePath);
		}

		await this.MigrateAsync(cancellationToken).ConfigureAwait(false);
	}

	/// <inheritdoc />
	public async Task MigrateAsync(CancellationToken cancellationToken = default) {
		await using var db = await this._dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

		var created = await db.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
		if (created) {
			await this.EnsureInitialDbVersionRecordAsync(db, cancellationToken).ConfigureAwait(false);
			this._logger.LogInformation("新規データベースを作成しました (Version={Version})。", this._latestVersion);
			return;
		}

		var dbVersion = await db.DbVersions
			.FirstOrDefaultAsync(x => x.Id == DatabaseMigrationRegistry.DbVersionRecordId, cancellationToken)
			.ConfigureAwait(false);
		var currentVersion = dbVersion?.Version ?? DatabaseMigrationRegistry.InitialVersion;

		if (currentVersion > this._latestVersion) {
			this._logger.LogWarning(
				"データベースのバージョン ({CurrentVersion}) がこのアプリケーションの対応バージョン ({LatestVersion}) より新しいため、マイグレーションをスキップします。",
				currentVersion,
				this._latestVersion);
			return;
		}

		if (currentVersion == this._latestVersion) {
			if (dbVersion == null) {
				await this.EnsureInitialDbVersionRecordAsync(db, cancellationToken).ConfigureAwait(false);
			}
			return;
		}

		var pendingMigrations = this._migrations
			.Where(m => m.TargetVersion > currentVersion && m.TargetVersion <= this._latestVersion)
			.ToArray();

		foreach (var migration in pendingMigrations) {
			this._logger.LogInformation(
				"データベースマイグレーションを実行します (TargetVersion={TargetVersion}): {Description}",
				migration.TargetVersion,
				migration.Description);

			await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
			try {
				await migration.ApplyAsync(db, cancellationToken).ConfigureAwait(false);

				if (dbVersion == null) {
					dbVersion = new DbVersion {
						Id = DatabaseMigrationRegistry.DbVersionRecordId,
						Version = migration.TargetVersion
					};
					db.DbVersions.Add(dbVersion);
				} else {
					dbVersion.Version = migration.TargetVersion;
				}

				await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
				await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
			} catch {
				await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
				db.ChangeTracker.Clear();
				throw;
			}
		}

		this._logger.LogInformation(
			"データベースマイグレーションが完了しました ({FromVersion} -> {ToVersion})。",
			currentVersion,
			this._latestVersion);
	}

	/// <summary>
	/// 新規作成またはバージョンレコード未登録の最新 DB に対して初期バージョンレコードを登録します。
	/// </summary>
	/// <param name="db">データベースコンテキスト。</param>
	/// <param name="cancellationToken">キャンセルトークン。</param>
	private async Task EnsureInitialDbVersionRecordAsync(MediaDeckDbContext db, CancellationToken cancellationToken) {
		await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
		try {
			var existing = await db.DbVersions
				.FirstOrDefaultAsync(x => x.Id == DatabaseMigrationRegistry.DbVersionRecordId, cancellationToken)
				.ConfigureAwait(false);
			if (existing == null) {
				db.DbVersions.Add(new DbVersion {
					Id = DatabaseMigrationRegistry.DbVersionRecordId,
					Version = this._latestVersion
				});
				await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
			}
			await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
		} catch {
			await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
			db.ChangeTracker.Clear();
			throw;
		}
	}
}