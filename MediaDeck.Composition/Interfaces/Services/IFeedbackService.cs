using MediaDeck.Composition.Objects;

namespace MediaDeck.Composition.Interfaces.Services;

/// <summary>
/// フィードバックの収集・送信を行うサービスインターフェース
/// </summary>
public interface IFeedbackService {
	/// <summary>
	/// アプリケーションおよび実行環境の情報をテキスト形式で取得します。
	/// </summary>
	/// <returns>環境情報文字列</returns>
	public string GetEnvironmentInfo();

	/// <summary>
	/// ログディレクトリ内の最新ログファイルから末尾の指定行数を取得します。
	/// </summary>
	/// <param name="maxLines">取得する最大行数</param>
	/// <param name="cancellationToken">キャンセルトークン</param>
	/// <returns>直近のログテキスト</returns>
	public Task<string> GetRecentLogsAsync(int maxLines = 100, CancellationToken cancellationToken = default);

	/// <summary>
	/// フィードバックをサーバーへ送信します。
	/// </summary>
	/// <param name="payload">送信するフィードバックペイロード</param>
	/// <param name="cancellationToken">キャンセルトークン</param>
	/// <returns>送信に成功した場合は <c>true</c>、失敗した場合は <c>false</c></returns>
	public Task<bool> SendFeedbackAsync(FeedbackPayload payload, CancellationToken cancellationToken = default);
}