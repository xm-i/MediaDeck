namespace MediaDeck.Composition.Database.Migrations;

/// <summary>
/// データベースマイグレーションのバージョン定義とステップ一覧を管理します。
/// </summary>
public static class DatabaseMigrationRegistry {
	/// <summary>
	/// 初期データベースバージョン。
	/// </summary>
	public const int InitialVersion = 1;

	/// <summary>
	/// <c>DbVersions</c> テーブルで管理するレコードの固定 ID。
	/// </summary>
	public const long DbVersionRecordId = 1;

	/// <summary>
	/// 現在の最新データベースバージョン。
	/// </summary>
	public const int LatestVersion = 2;

	/// <summary>
	/// 登録されているマイグレーションステップ一覧を取得します。
	/// </summary>
	public static IReadOnlyList<IDatabaseMigration> Migrations {
		get;
	} = [
		new Migration002AddMediaItemSystemIndexes()
	];
}