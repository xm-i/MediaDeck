using System.Text.Json.Serialization;

namespace MediaDeck.ViewModels.Help;

/// <summary>
/// ライセンス情報のJSONシリアライズコンテキスト
/// </summary>
[JsonSerializable(typeof(List<LicenseInfo>))]
internal sealed partial class LicenseJsonSerializerContext : JsonSerializerContext {
}