using MediaDeck.Composition.Database;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MediaDeck.Core.Tests;

/// <summary>
/// テスト用の名前付き共有インメモリ SQLite データベースファクトリ。
/// インスタンスのライフタイム中キープアライブ接続を保持し、複数の <see cref="MediaDeckDbContext"/> 間での読み書きや並行利用を可能にします。
/// </summary>
public sealed class SqliteTestDbFactory : IDbContextFactory<MediaDeckDbContext>, IDisposable {
	private readonly SqliteConnection _keepAliveConnection;
	private readonly DbContextOptions<MediaDeckDbContext> _options;

	/// <summary>
	/// <see cref="SqliteTestDbFactory"/> クラスの新しいインスタンスを初期化し、スキーマを作成します。
	/// </summary>
	public SqliteTestDbFactory() {
		var connectionString = $"Data Source=MediaDeckTest_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
		this._keepAliveConnection = new SqliteConnection(connectionString);
		this._keepAliveConnection.Open();

		this._options = new DbContextOptionsBuilder<MediaDeckDbContext>()
			.UseSqlite(connectionString)
			.Options;

		using var db = new MediaDeckDbContext(this._options);
		db.Database.EnsureCreated();
	}

	/// <summary>
	/// テスト用データベースに接続された <see cref="MediaDeckDbContext"/> を生成します。
	/// </summary>
	/// <returns>新しい <see cref="MediaDeckDbContext"/> インスタンス</returns>
	public MediaDeckDbContext CreateDbContext() {
		return new MediaDeckDbContext(this._options);
	}

	/// <summary>
	/// テスト用データベースに接続された <see cref="MediaDeckDbContext"/> を非同期で生成します。
	/// </summary>
	/// <param name="cancellationToken">キャンセルトークン</param>
	/// <returns>新しい <see cref="MediaDeckDbContext"/> インスタンス</returns>
	public Task<MediaDeckDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) {
		cancellationToken.ThrowIfCancellationRequested();
		return Task.FromResult(new MediaDeckDbContext(this._options));
	}

	/// <inheritdoc />
	public void Dispose() {
		this._keepAliveConnection.Dispose();
	}
}