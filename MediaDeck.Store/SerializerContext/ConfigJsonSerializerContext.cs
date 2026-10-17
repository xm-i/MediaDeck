using System.Text.Json.Serialization;
using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Core.Models.Files.Filter.FilterItemObjects;
using MediaDeck.MediaItemTypes.Base.Models;
using MediaDeck.MediaItemTypes.FolderGroup.Models;
using MediaDeck.Store.Converters;

namespace MediaDeck.Store.SerializerContext;

[JsonSourceGenerationOptions(
	WriteIndented = true,
	Converters = [typeof(GuidJsonConverter)]
)]
[JsonSerializable(typeof(ConfigModelForJson))]
[JsonSerializable(typeof(SearchDefinitionsConfigModelForJson))]
[JsonSerializable(typeof(IFilterItemObjectForJson))]
[JsonSerializable(typeof(ExistsFilterItemObjectForJson))]
[JsonSerializable(typeof(FilePathFilterItemObjectForJson))]
[JsonSerializable(typeof(LocationFilterItemObjectForJson))]
[JsonSerializable(typeof(MediaTypeFilterItemObjectForJson))]
[JsonSerializable(typeof(RateFilterItemObjectForJson))]
[JsonSerializable(typeof(TagFilterItemObjectForJson))]
[JsonSerializable(typeof(ResolutionFilterItemObjectForJson))]
[JsonSerializable(typeof(FolderGroupFilterItemObjectForJson))]
[JsonSerializable(typeof(IExecutionProgramObjectModelForJson))]
[JsonSerializable(typeof(DefaultExecutionProgramObjectModelForJson))]
[JsonSerializable(typeof(FolderGroupExecutionProgramObjectModelForJson))]
public partial class ConfigJsonSerializerContext : JsonSerializerContext {
}