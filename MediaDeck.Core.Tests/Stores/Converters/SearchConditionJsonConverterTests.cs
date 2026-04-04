using System.Text;
using System.Text.Json;

using FluentAssertions;

using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Core.Models.Files.SearchConditions;
using MediaDeck.Core.Stores.Converters;

namespace MediaDeck.Core.Tests.Stores.Converters;

public class SearchConditionJsonConverterTests {
	private readonly JsonSerializerOptions _options;

	public SearchConditionJsonConverterTests() {
		this._options = new JsonSerializerOptions();
		this._options.Converters.Add(new SearchConditionJsonConverter());
	}

	[Fact]
	public void Read_ValidXaml_ReturnsSearchCondition() {
		// Arrange
		var word = "testWord";
		var xaml = $@"<WordSearchCondition Word=""{word}"" xmlns=""clr-namespace:MediaDeck.Core.Models.Files.SearchConditions;assembly=MediaDeck.Core"" />";
		var json = $@"""{xaml.Replace(@"""", @"\""")}""";

		// Act
		var result = JsonSerializer.Deserialize<ISearchCondition>(json, this._options);

		// Assert
		result.Should().BeOfType<WordSearchCondition>();
		result.As<WordSearchCondition>().Word.Should().Be(word);
	}

	[Fact]
	public void Read_NullJson_ReturnsNull() {
		// Arrange
		var json = "null";

		// Act
		var result = JsonSerializer.Deserialize<ISearchCondition>(json, this._options);

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public void Read_InvalidTypeXaml_ThrowsException() {
		// Arrange
		// Use a type that is NOT ISearchCondition
		var xaml = @"<string xmlns=""http://schemas.microsoft.com/winfx/2006/xaml"">test</string>";
		var json = $@"""{xaml.Replace(@"""", @"\""")}""";

		// Act
		var action = () => JsonSerializer.Deserialize<ISearchCondition>(json, this._options);

		// Assert
		action.Should().Throw<JsonException>()
			.WithInnerException<Exception>()
			.And.InnerException!.Message.Should().Be("Failed to deserialize ISearchCondition from XAML.");
	}

	[Fact]
	public void Write_ValidSearchCondition_SerializesToXaml() {
		// Arrange
		var word = "testWord";
		var condition = new WordSearchCondition(word);

		// Act
		var json = JsonSerializer.Serialize<ISearchCondition>(condition, this._options);

		// Assert
		json.Should().Contain("WordSearchCondition");
		json.Should().Contain(word);

		// Round trip
		var deserialized = JsonSerializer.Deserialize<ISearchCondition>(json, this._options);
		deserialized.Should().BeOfType<WordSearchCondition>();
		deserialized.As<WordSearchCondition>().Word.Should().Be(word);
	}
}
