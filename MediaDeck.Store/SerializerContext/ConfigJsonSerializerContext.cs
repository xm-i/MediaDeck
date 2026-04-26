using System.Text.Json.Serialization;

using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.MediaItemTypes.Base.Models;
using MediaDeck.MediaItemTypes.FolderGroup.Models;

namespace MediaDeck.Stores.SerializerContext;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(ConfigModelForJson))]
[JsonSerializable(typeof(IExecutionProgramObjectModelForJson))]
[JsonSerializable(typeof(DefaultExecutionProgramObjectModelForJson))]
[JsonSerializable(typeof(FolderGroupExecutionProgramObjectModelForJson))]
public partial class ConfigJsonSerializerContext : JsonSerializerContext {
}