namespace MediaDeck.Core.Models.NotificationDispatcher;

/// <summary>
/// アルバム変更に関するグローバル通知を提供します。
/// </summary>
public static class AlbumNotifications {
	/// <summary>
	/// アルバム情報が更新されたときに発行される通知ストリーム。
	/// 送信元の <see cref="object"/>（通常は <see cref="Repositories.AlbumRepository"/> インスタンス）が渡されます。
	/// </summary>
	public static Subject<object?> AlbumUpdated {
		get;
	} = new();
}