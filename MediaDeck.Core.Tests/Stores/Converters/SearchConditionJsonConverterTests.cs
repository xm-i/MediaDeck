using System.Text;
using System.Text.Json;

using FluentAssertions;

using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Core.Models.Files.SearchConditions;
using MediaDeck.Core.Stores.Converters;

namespace MediaDeck.Core.Tests.Stores.Converters;

/// <summary>
/// <see cref="SearchConditionJsonConverter"/> のテストクラスです。
/// </summary>
public class SearchConditionJsonConverterTests {
	private readonly JsonSerializerOptions _options;

	/// <summary>
	/// テストを初期化します。
	/// </summary>
	public SearchConditionJsonConverterTests() {
		this._options = new JsonSerializerOptions();
		this._options.Converters.Add(new SearchConditionJsonConverter());
	}

	/// <summary>
	/// 有効な XAML から ISearchCondition を正常にデシリアライズできることを確認します。
	/// </summary>
	[Fact]
	public void Read_ValidXaml_ReturnsSearchCondition() {
		// Arrange
		var word = "testWord";
		var xaml = $@"<WordSearchCondition Word=""{word}"" xmlns=""clr-namespace:MediaDeck.Core.Models.Files.SearchConditions;assembly=MediaDeck.Core"" />";
		var json = $@"""{xaml.Replace(@"""", @"\""")}""";

		// Act
		var result = JsonSerializer.Deserialize<ISearchCondition>(json, this._options);

		// Assert
		result.Should().NotBeNull();
		result.Should().BeOfType<WordSearchCondition>();
		((WordSearchCondition)result!).Word.Should().Be(word);
	}

	/// <summary>
	/// JSON が null の場合に null を返すことを確認します。
	/// </summary>
	[Fact]
	public void Read_NullJson_ReturnsNull() {
		// Arrange
		var json = "null";

		// Act
		var result = JsonSerializer.Deserialize<ISearchCondition>(json, this._options);

		// Assert
		result.Should().BeNull();
	}

	/// <summary>
	/// ISearchCondition ではない型の XAML の場合に例外がスローされることを確認します。
	/// </summary>
	[Fact]
	public void Read_InvalidTypeXaml_ThrowsException() {
		// Arrange
		// ISearchCondition ではない型（string など）を使用
		var xaml = @"<String xmlns=""http://schemas.microsoft.com/winfx/2006/xaml"">test</String>";
		var json = $@"""{xaml.Replace(@"""", @"\""")}""";

		// Act
		var action = () => JsonSerializer.Deserialize<ISearchCondition>(json, this._options);

		// Assert
		// JsonSerializer は Converter から投げられた Exception を JsonException でラップする
		action.Should().Throw<JsonException>()
			.WithInnerException<Exception>()
			.And.InnerException!.Message.Should().Be("Failed to deserialize ISearchCondition from XAML.");
	}

	/// <summary>
	/// 有効な ISearchCondition を正常にシリアライズおよびデシリアライズ（ラウンドトリップ）できることを確認します。
	/// </summary>
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
		deserialized.Should().NotBeNull();
		deserialized.Should().BeOfType<WordSearchCondition>();
		((WordSearchCondition)deserialized!).Word.Should().Be(word);
	}
}
