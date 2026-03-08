using MediaDeck.Composition.Objects;

namespace MediaDeck.Utils.Notifications;

/// <summary>
/// アプリケーション全体の通知を配信するディスパッチャー（UI非依存）
/// </summary>
[Inject(InjectServiceLifetime.Singleton)]
public class AppNotificationDispatcher {
	/// <summary>
	/// 通知を発行するSubject
	/// </summary>
	public Subject<AppNotification> Notify {
		get;
	} = new();
}
