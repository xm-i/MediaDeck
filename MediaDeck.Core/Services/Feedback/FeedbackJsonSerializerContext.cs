using System.Text.Json.Serialization;
using MediaDeck.Composition.Objects;

namespace MediaDeck.Core.Services.Feedback;

/// <summary>
/// フィードバック送信ペイロード用のJSONシリアライズコンテキスト
/// </summary>
[JsonSourceGenerationOptions(
	PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
	DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(FeedbackPayload))]
internal sealed partial class FeedbackJsonSerializerContext : JsonSerializerContext {
}