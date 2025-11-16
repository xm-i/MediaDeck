using System.Text.Json.Serialization;

using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Stores.Converters;

namespace MediaDeck.Stores.SerializerContext;

[JsonSourceGenerationOptions(WriteIndented = true,Converters = [
			typeof(GuidJsonConverter),
			typeof(FilterItemObjectJsonConverter),
			typeof(SearchConditionJsonConverter)])]
[JsonSerializable(typeof(StateModelForJson))]
public partial class StateJsonSerializerContext : JsonSerializerContext {
}
