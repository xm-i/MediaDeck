using System.Text.Json.Serialization;

namespace MediaDeck.Core.Services.Update;

/// <summary>
/// GitHub Releases API レスポンス用のJSONシリアライズコンテキスト
/// </summary>
[JsonSerializable(typeof(GitHubReleaseResponse))]
internal sealed partial class GitHubReleaseJsonSerializerContext : JsonSerializerContext {
}