namespace MediaDeck.Composition.Objects;

/// <summary>
/// 利用可能なアプリケーション更新情報
/// </summary>
public record AppUpdateInfo {
	/// <summary>
	/// GitHubリリースのタグ名（例: v1.2.0）
	/// </summary>
	public required string TagName {
		get;
		init;
	}

	/// <summary>
	/// 正規化された最新バージョンの表示文字列（例: 1.2.0）
	/// </summary>
	public required string VersionText {
		get;
		init;
	}

	/// <summary>
	/// 現在のアプリケーションバージョンの表示文字列（例: 1.1.0）
	/// </summary>
	public required string CurrentVersionText {
		get;
		init;
	}

	/// <summary>
	/// リリースタイトル
	/// </summary>
	public required string ReleaseName {
		get;
		init;
	}

	/// <summary>
	/// リリースノート本文
	/// </summary>
	public required string ReleaseNotes {
		get;
		init;
	}

	/// <summary>
	/// リリースページのURL
	/// </summary>
	public required string HtmlUrl {
		get;
		init;
	}

	/// <summary>
	/// 公開日時
	/// </summary>
	public DateTimeOffset? PublishedAt {
		get;
		init;
	}
}

/// <summary>
/// 更新確認の結果ステータス
/// </summary>
public enum UpdateCheckStatus {
	/// <summary>
	/// 現在のバージョンが最新です。
	/// </summary>
	UpToDate,

	/// <summary>
	/// 新しいバージョンが利用可能です。
	/// </summary>
	UpdateAvailable,

	/// <summary>
	/// 現在の実行形態ではアプリ内更新確認に対応していません。
	/// </summary>
	NotSupported,

	/// <summary>
	/// 更新確認に失敗しました。
	/// </summary>
	Failed,
}

/// <summary>
/// 更新確認の結果
/// </summary>
/// <param name="Status">確認結果ステータス</param>
/// <param name="UpdateInfo">利用可能な更新情報（更新がある場合のみ）</param>
public record UpdateCheckResult(UpdateCheckStatus Status, AppUpdateInfo? UpdateInfo = null);