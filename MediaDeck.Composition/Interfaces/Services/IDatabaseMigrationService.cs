namespace MediaDeck.Composition.Interfaces.Services;

/// <summary>
/// データベースの初期化およびバージョンに応じたマイグレーションを実行するサービスインターフェース。
/// </summary>
public interface IDatabaseMigrationService {
	/// <summary>
	/// 指定されたファイルパスのデータベースを初期化し、必要に応じてマイグレーションを適用します。
	/// </summary>
	/// <param name="databaseFilePath">データベースファイルのパス。</param>
	/// <param name="cancellationToken">キャンセルトークン。</param>
	/// <returns>初期化およびマイグレーション処理を表すタスク。</returns>
	public Task InitializeAsync(string databaseFilePath, CancellationToken cancellationToken = default);

	/// <summary>
	/// 現在のデータベースに対して未適用のマイグレーションを実行します。
	/// </summary>
	/// <param name="cancellationToken">キャンセルトークン。</param>
	/// <returns>マイグレーション処理を表すタスク。</returns>
	public Task MigrateAsync(CancellationToken cancellationToken = default);
}