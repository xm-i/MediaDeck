using System;
using System.Text.Json;
using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Core.Stores.Converters;
using Xunit;

namespace MediaDeck.Core.Tests.Stores.Converters;

public class FilterItemObjectJsonConverterTests {
	private class TestContainer {
		public IFilterItemObject? Item {
			get; set;
		}
	}

	[Fact]
	public void Read_InvalidType_ThrowsException() {
		// Arrange
		// Give it valid XAML but of a type that does not implement IFilterItemObject.
		var json = "{\"Item\":\"<Int32 xmlns=\\\"clr-namespace:System;assembly=System.Runtime\\\">42</Int32>\"}";
		var options = new JsonSerializerOptions {
			Converters = { new FilterItemObjectJsonConverter() }
		};

		// Act & Assert
		var exception = Assert.Throws<Exception>(() => JsonSerializer.Deserialize<TestContainer>(json, options));
		Assert.Equal("Failed to deserialize IFilterItemObject from XAML.", exception.Message);
	}

	[Fact]
	public void Read_InvalidXaml_ThrowsException() {
		// Arrange
		// Invalid XAML, which throws during XamlServices.Load
		var json = "{\"Item\":\"<InvalidXaml>not a filter item</InvalidXaml>\"}";
		var options = new JsonSerializerOptions {
			Converters = { new FilterItemObjectJsonConverter() }
		};

		// Act & Assert
		var exception = Record.Exception(() => JsonSerializer.Deserialize<TestContainer>(json, options));
		Assert.NotNull(exception);
	}
}