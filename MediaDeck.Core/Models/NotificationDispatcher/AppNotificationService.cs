using MediaDeck.Composition.Objects;

namespace MediaDeck.Core.Models.NotificationDispatcher;

/// <summary>
/// 現在のコンテキスト（DIスコープ）に基づいて通知をルーティングするサービスの実装
/// </summary>
[Inject(InjectServiceLifetime.Transient, typeof(IAppNotificationService))]
public class AppNotificationService(
	AppNotificationDispatcher dispatcher,
	NotificationContextProvider contextProvider
) : IAppNotificationService {
	/// <inheritdoc />
	public void Notify(AppNotification notification) {
		// 現在のスコープに応じたTargetWindowIdを取得
		var targetId = contextProvider.GetTargetWindowId();

		// IDを付与してディスパッチャーへ送信
		dispatcher.Notify.OnNext(notification with {
			TargetWindowId = targetId
		});
	}
}