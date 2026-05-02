using MediaDeck.Composition.Objects;

namespace MediaDeck.Composition.Interfaces.Services;

/// <summary>
/// アプリケーション通知を送信するためのサービスインターフェース
/// </summary>
public interface IAppNotificationService {
	/// <summary>
	/// 通知を送信します。
	/// </summary>
	/// <param name="notification">送信する通知内容</param>
	public void Notify(AppNotification notification);
}