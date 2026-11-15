namespace MediaDeck.Composition.Objects;

/// <summary>
/// フィードバック送信用のJSONペイロード
/// </summary>
public record FeedbackPayload {
	/// <summary>
	/// フィードバック種別
	/// </summary>
	public required string Category {
		get;
		init;
	}

	/// <summary>
	/// 件名
	/// </summary>
	public required string Title {
		get;
		init;
	}

	/// <summary>
	/// 詳細内容
	/// </summary>
	public required string Description {
		get;
		init;
	}

	/// <summary>
	/// 返信先・連絡先（任意）
	/// </summary>
	public string? Contact {
		get;
		init;
	}

	/// <summary>
	/// 環境情報
	/// </summary>
	public string? EnvironmentInfo {
		get;
		init;
	}

	/// <summary>
	/// 直近のログ内容
	/// </summary>
	public string? LogContent {
		get;
		init;
	}

	/// <summary>
	/// 添付スクリーンショットのBase64文字列
	/// </summary>
	public string? ScreenshotBase64 {
		get;
		init;
	}

	/// <summary>
	/// 送信日時
	/// </summary>
	public DateTimeOffset SubmittedAt {
		get;
		init;
	}
}