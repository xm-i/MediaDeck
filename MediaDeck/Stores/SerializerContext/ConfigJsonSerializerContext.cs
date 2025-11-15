using System.Text.Json.Serialization;

using MediaDeck.Composition.Stores.Config.Model;

namespace MediaDeck.Stores.SerializerContext;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(ConfigModelForJson))]
public partial class ConfigJsonSerializerContext : JsonSerializerContext {
}
