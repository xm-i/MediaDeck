using System.Text.Json.Serialization;

namespace MediaDeck.Core.Services.Update;

/// <summary>
/// GitHub Releases API のレスポンスDTO
/// </summary>
internal sealed record GitHubReleaseResponse {
	/// <summary>
	/// タグ名
	/// </summary>
	[JsonPropertyName("tag_name")]
	public string? TagName {
		get;
		init;
	}

	/// <summary>
	/// リリース名
	/// </summary>
	[JsonPropertyName("name")]
	public string? Name {
		get;
		init;
	}

	/// <summary>
	/// リリースページのHTML URL
	/// </summary>
	[JsonPropertyName("html_url")]
	public string? HtmlUrl {
		get;
		init;
	}

	/// <summary>
	/// リリースノート本文
	/// </summary>
	[JsonPropertyName("body")]
	public string? Body {
		get;
		init;
	}

	/// <summary>
	/// 公開日時
	/// </summary>
	[JsonPropertyName("published_at")]
	public DateTimeOffset? PublishedAt {
		get;
		init;
	}

	/// <summary>
	/// 下書きかどうか
	/// </summary>
	[JsonPropertyName("draft")]
	public bool Draft {
		get;
		init;
	}

	/// <summary>
	/// プレリリースかどうか
	/// </summary>
	[JsonPropertyName("prerelease")]
	public bool Prerelease {
		get;
		init;
	}
}