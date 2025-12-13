using System.Text.Json;
using System.Text.Json.Serialization;

namespace MediaDeck.Stores.Converters; 
public class GuidJsonConverter: JsonConverter<Guid> {
	public override Guid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
		var str = reader.GetString();
		if (str == null) {
			return Guid.NewGuid();
		}
		var guid = Guid.Parse(str);
		return guid;
	}

	public override void Write(Utf8JsonWriter writer, Guid value, JsonSerializerOptions options) {
		writer.WriteStringValue(value.ToString());
	}
}
