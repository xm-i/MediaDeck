using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Core.Stores.Converters;
using Portable.Xaml;
using Xunit;

namespace MediaDeck.Core.Tests.Stores.Converters;

public class FilterItemObjectJsonConverterTests
{
    private readonly JsonSerializerOptions _options;

    public FilterItemObjectJsonConverterTests()
    {
        _options = new JsonSerializerOptions();
        _options.Converters.Add(new FilterItemObjectJsonConverter());
    }

    [Fact]
    public void Read_ThrowsException_WhenXamlIsNotFilterItemObject()
    {
        // Arrange
        // Create valid XAML that is NOT an IFilterItemObject
        var invalidXaml = "<String xmlns=\"clr-namespace:System;assembly=mscorlib\">This is not an IFilterItemObject</String>";
        var json = JsonSerializer.Serialize(invalidXaml); // Serialize as string to be read by reader.GetString()

        // Act
        Action act = () => JsonSerializer.Deserialize<IFilterItemObject>(json, _options);

        // Assert
        act.Should().Throw<Exception>()
            .WithMessage("Failed to deserialize IFilterItemObject from XAML.");
    }
}
