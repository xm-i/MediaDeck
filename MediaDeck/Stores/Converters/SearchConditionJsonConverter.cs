using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xaml;

using MediaDeck.Composition.Interfaces.Files;

namespace MediaDeck.Stores.Converters; 
public class SearchConditionJsonConverter : JsonConverter<ISearchCondition?> {
	public override ISearchCondition? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
		var xml = reader.GetString();
		if (xml is null) {
			return null;
		}

		var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
		if (XamlServices.Load(stream) is not ISearchCondition fio) {
			throw new Exception("Failed to deserialize ISearchCondition from XAML.");
		}
		return fio;
	}

	public override void Write(Utf8JsonWriter writer, ISearchCondition? value, JsonSerializerOptions options) {
		using var ms = new MemoryStream();
		XamlServices.Save(ms, value);
		writer.WriteStringValue(Encoding.UTF8.GetString(ms.ToArray()));
	}
}
