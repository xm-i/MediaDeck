using Microsoft.EntityFrameworkCore;

namespace MediaDeck.Composition.Database.Migrations;

/// <summary>
/// <c>MediaItems</c> テーブルにシステム共通クエリ用のインデックスを追加するマイグレーション（バージョン 2）。
/// </summary>
public sealed class Migration002AddMediaItemSystemIndexes : IDatabaseMigration {
	/// <inheritdoc />
	public int TargetVersion {
		get {
			return 2;
		}
	}

	/// <inheritdoc />
	public string Description {
		get {
			return "MediaItems テーブルへシステム共通クエリ用インデックス（DirectoryPath, PreHash+IsExists）を追加";
		}
	}

	/// <inheritdoc />
	public async Task ApplyAsync(MediaDeckDbContext dbContext, CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(dbContext);

		await dbContext.Database.ExecuteSqlRawAsync(
			"""CREATE INDEX IF NOT EXISTS "IX_MediaItems_DirectoryPath" ON "MediaItems" ("DirectoryPath");""",
			cancellationToken).ConfigureAwait(false);
		await dbContext.Database.ExecuteSqlRawAsync(
			"""CREATE INDEX IF NOT EXISTS "IX_MediaItems_PreHash_IsExists" ON "MediaItems" ("PreHash", "IsExists");""",
			cancellationToken).ConfigureAwait(false);
	}
}