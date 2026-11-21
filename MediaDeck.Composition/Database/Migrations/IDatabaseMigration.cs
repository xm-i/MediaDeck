namespace MediaDeck.Composition.Database.Migrations;

/// <summary>
/// データベーススキーマまたはデータのマイグレーションステップを定義します。
/// </summary>
public interface IDatabaseMigration {
	/// <summary>
	/// このマイグレーション適用後の目標データベースバージョンを取得します。
	/// </summary>
	public int TargetVersion {
		get;
	}

	/// <summary>
	/// マイグレーション内容の説明を取得します。
	/// </summary>
	public string Description {
		get;
	}

	/// <summary>
	/// マイグレーションを適用します。
	/// </summary>
	/// <param name="dbContext">対象データベースコンテキスト。</param>
	/// <param name="cancellationToken">キャンセルトークン。</param>
	/// <returns>適用処理を表すタスク。</returns>
	public Task ApplyAsync(MediaDeckDbContext dbContext, CancellationToken cancellationToken = default);
}