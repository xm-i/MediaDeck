using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Composition.Interfaces.Primitives;
using MediaDeck.Composition.Objects;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Core.Models.Files;
using MediaDeck.Core.Models.Files.Filter.FilterItemObjects;
using MediaDeck.Core.Models.Files.SearchConditions;
using MediaDeck.Core.Models.Maps;
using MediaDeck.Core.Stores.Converters;

namespace MediaDeck.Stores.SerializerContext;

[JsonSourceGenerationOptions(
	WriteIndented = true,
	Converters = [typeof(GuidJsonConverter)]
)]
[JsonSerializable(typeof(StateModelForJson))]
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
public partial class StateJsonSerializerContext : JsonSerializerContext {
	/// <summary>
	/// 実行時の型レジストリ設定を適用した JsonSerializerOptions。
	/// </summary>
	public static JsonSerializerOptions DefaultOptions {
		get;
	}

	static StateJsonSerializerContext() {
		DefaultOptions = new JsonSerializerOptions(Default.Options) {
			TypeInfoResolver = Default.WithAddedModifier(global::R3.JsonConfig.ForJsonConverterRegistry.ApplyPolymorphism)
		};
	}
}
