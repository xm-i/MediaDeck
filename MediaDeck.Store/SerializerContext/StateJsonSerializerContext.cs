using System.Text.Json.Serialization;
using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Core.Models.Files.Filter.FilterItemObjects;
using MediaDeck.Core.Models.Files.SearchConditions;
using MediaDeck.Core.Models.Tags;
using MediaDeck.Store.Converters;

namespace MediaDeck.Stores.SerializerContext;

[JsonSourceGenerationOptions(
	WriteIndented = true,
	Converters = [typeof(GuidJsonConverter)]
)]
[JsonSerializable(typeof(RootStateModelForJson))]
[JsonSerializable(typeof(AppStateModelForJson))]
[JsonSerializable(typeof(TabStateModelForJson))]
[JsonSerializable(typeof(SearchDefinitionsStateModelForJson))]
[JsonSerializable(typeof(ISearchConditionForJson))]
[JsonSerializable(typeof(AddressSearchConditionForJson))]
[JsonSerializable(typeof(FolderSearchConditionForJson))]
[JsonSerializable(typeof(TagSearchConditionForJson))]
[JsonSerializable(typeof(WordSearchConditionForJson))]
[JsonSerializable(typeof(IFilterItemObjectForJson))]
[JsonSerializable(typeof(ExistsFilterItemObjectForJson))]
[JsonSerializable(typeof(FilePathFilterItemObjectForJson))]
[JsonSerializable(typeof(LocationFilterItemObjectForJson))]
[JsonSerializable(typeof(MediaTypeFilterItemObjectForJson))]
[JsonSerializable(typeof(RateFilterItemObjectForJson))]
[JsonSerializable(typeof(TagFilterItemObjectForJson))]
[JsonSerializable(typeof(ResolutionFilterItemObjectForJson))]
[JsonSerializable(typeof(TagModelForJson))]
[JsonSerializable(typeof(TagAliasModelForJson))]
[JsonSerializable(typeof(TagCategoryModelForJson))]
public partial class StateJsonSerializerContext : JsonSerializerContext {
	static StateJsonSerializerContext() {
	}
}