using MediaDeck.Composition.Objects;

namespace MediaDeck.Core.Models.NotificationDispatcher;

/// <summary>
/// InfoBarに表示する通知メッセージ（UI層の具体実装）
/// </summary>
public record InfoBarNotification {
	/// <summary>
	/// 通知のタイトル
	/// </summary>
	public string? Title {
		get;
		init;
	}

	/// <summary>
	/// 通知のメッセージ
	/// </summary>
	public required string Message {
		get;
		init;
	}

	/// <summary>
	/// 通知の重要度
	/// </summary>
	public NotificationSeverity Severity {
		get;
		init;
	} = NotificationSeverity.Informational;

	/// <summary>
	/// 自動的に閉じるまでの時間（ミリ秒）。0以下の場合は自動で閉じない。
	/// </summary>
	public int AutoCloseMilliseconds {
		get;
		init;
	} = 3000;

	/// <summary>
	/// AppNotificationからInfoBarNotificationに変換
	/// </summary>
	/// <param name="appNotification">変換元のアプリケーション通知</param>
	/// <returns>変換されたInfoBar通知</returns>
	public static InfoBarNotification FromAppNotification(AppNotification appNotification) {
		return new() {
			Message = appNotification.Message,
			Title = appNotification.Title,
			Severity = appNotification.Severity,
			AutoCloseMilliseconds = appNotification.AutoCloseMilliseconds
		};
	}

	/// <summary>
	/// 情報通知を作成
	/// </summary>
	/// <param name="message">通知メッセージ</param>
	/// <param name="title">通知タイトル（省略可能）</param>
	/// <param name="autoCloseMs">自動クローズ時間（ミリ秒）。デフォルトは3000ms</param>
	/// <returns>情報レベルの通知</returns>
	public static InfoBarNotification Info(string message, string? title = null, int autoCloseMs = 3000) {
		return new() {
			Message = message,
			Title = title,
			Severity = NotificationSeverity.Informational,
			AutoCloseMilliseconds = autoCloseMs
		};
	}

	/// <summary>
	/// 成功通知を作成
	/// </summary>
	/// <param name="message">通知メッセージ</param>
	/// <param name="title">通知タイトル（省略可能）</param>
	/// <param name="autoCloseMs">自動クローズ時間（ミリ秒）。デフォルトは3000ms</param>
	/// <returns>成功レベルの通知</returns>
	public static InfoBarNotification Success(string message, string? title = null, int autoCloseMs = 3000) {
		return new() {
			Message = message,
			Title = title,
			Severity = NotificationSeverity.Success,
			AutoCloseMilliseconds = autoCloseMs
		};
	}

	/// <summary>
	/// 警告通知を作成
	/// </summary>
	/// <param name="message">通知メッセージ</param>
	/// <param name="title">通知タイトル（省略可能）</param>
	/// <param name="autoCloseMs">自動クローズ時間（ミリ秒）。デフォルトは5000ms</param>
	/// <returns>警告レベルの通知</returns>
	public static InfoBarNotification Warning(string message, string? title = null, int autoCloseMs = 5000) {
		return new() {
			Message = message,
			Title = title,
			Severity = NotificationSeverity.Warning,
			AutoCloseMilliseconds = autoCloseMs
		};
	}

	/// <summary>
	/// エラー通知を作成
	/// </summary>
	/// <param name="message">通知メッセージ</param>
	/// <param name="title">通知タイトル（省略可能）</param>
	/// <param name="autoCloseMs">自動クローズ時間（ミリ秒）。デフォルトは0（自動で閉じない）</param>
	/// <returns>エラーレベルの通知</returns>
	public static InfoBarNotification Error(string message, string? title = null, int autoCloseMs = 0) {
		return new() {
			Message = message,
			Title = title,
			Severity = NotificationSeverity.Error,
			AutoCloseMilliseconds = autoCloseMs
		};
	}
}