using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Core.Stores.Converters;
using Portable.Xaml;
using Shouldly;
using Xunit;

namespace MediaDeck.Core.Tests.Stores.Converters;

public class FilterItemObjectJsonConverterTests {
	private readonly JsonSerializerOptions _options;

	public FilterItemObjectJsonConverterTests() {
		this._options = new JsonSerializerOptions();
		this._options.Converters.Add(new FilterItemObjectJsonConverter());
	}

	[Fact]
	public void Read_ThrowsException_WhenXamlIsNotFilterItemObject() {
		// Arrange
		// Create valid XAML that is NOT an IFilterItemObject
		var invalidXaml = "<String xmlns=\"clr-namespace:System;assembly=mscorlib\">This is not an IFilterItemObject</String>";
		var json = JsonSerializer.Serialize(invalidXaml); // Serialize as string to be read by reader.GetString()

		// Act
		Action act = () => JsonSerializer.Deserialize<IFilterItemObject>(json, this._options);

		// Assert
		var exception = act.ShouldThrow<Exception>();
		exception.Message.ShouldBe("Failed to deserialize IFilterItemObject from XAML.");
	}
}