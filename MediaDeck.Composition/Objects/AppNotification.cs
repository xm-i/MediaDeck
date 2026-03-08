namespace MediaDeck.Composition.Objects;

/// <summary>
/// アプリケーション通知
/// </summary>
public record AppNotification {
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
	/// 情報通知を作成
	/// </summary>
	/// <param name="message">通知メッセージ</param>
	/// <param name="title">通知タイトル（省略可能）</param>
	/// <param name="autoCloseMs">自動クローズ時間（ミリ秒）。デフォルトは5000ms</param>
	/// <returns>情報レベルの通知</returns>
	public static AppNotification Info(string message, string? title = null, int autoCloseMs = 5000) {
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
	/// <param name="autoCloseMs">自動クローズ時間（ミリ秒）。デフォルトは5000ms</param>
	/// <returns>成功レベルの通知</returns>
	public static AppNotification Success(string message, string? title = null, int autoCloseMs = 5000) {
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
	public static AppNotification Warning(string message, string? title = null, int autoCloseMs = 5000) {
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
	public static AppNotification Error(string message, string? title = null, int autoCloseMs = 0) {
		return new() {
			Message = message,
			Title = title,
			Severity = NotificationSeverity.Error,
			AutoCloseMilliseconds = autoCloseMs
		};
	}
}
