namespace MediaDeck.Composition.Interfaces.Services;

/// <summary>
/// メディア再生エンジンの遅延初期化と終了時クリーンアップを管理するサービスインターフェース。
/// </summary>
public interface IMediaEngineService {
	/// <summary>
	/// メディア再生エンジンが初期化済みかどうかを取得します。
	/// </summary>
	public bool IsInitialized {
		get;
	}

	/// <summary>
	/// メディア再生エンジンが初期化されていることを保証します。未初期化の場合は遅延初期化を実行します。
	/// </summary>
	/// <param name="cancellationToken">呼び出し元の待機をキャンセルするためのキャンセルトークン。</param>
	/// <returns>エンジンが正常に初期化された場合は true、初期化に失敗した場合は false。</returns>
	public Task<bool> EnsureInitializedAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// 初期化済みのメディア再生エンジンおよび残存プレイヤーを安全に終了・解放します。
	/// </summary>
	public void Shutdown();
}