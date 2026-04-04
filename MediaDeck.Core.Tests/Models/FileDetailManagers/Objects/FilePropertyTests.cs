using System;
using System.Collections.Generic;
using MediaDeck.Core.Models.FileDetailManagers.Objects;
using MediaDeck.Core.Primitives;
using Xunit;

namespace MediaDeck.Core.Tests.Models.FileDetailManagers.Objects;

/// <summary>
/// FileProperty クラスのテスト
/// </summary>
public class FilePropertyTests {
	/// <summary>
	/// コンストラクタがプロパティを正しく設定することを確認するテスト
	/// </summary>
	[Fact]
	public void Constructor_SetsPropertiesCorrectly() {
		// Arrange
		var title = "Test Title";
		var values = new List<ValueCountPair<string?>>
		{
			new ValueCountPair<string?>("Value1", 1),
			new ValueCountPair<string?>("Value2", 2)
		};

		// Act
		var fileProperty = new FileProperty(title, values);

		// Assert
		Assert.Equal(title, fileProperty.Title);
		Assert.Same(values, fileProperty.Values);
	}

	/// <summary>
	/// RepresentativeValue が最初の値を返すことを確認するテスト
	/// </summary>
	[Fact]
	public void RepresentativeValue_ReturnsFirstValue() {
		// Arrange
		var title = "Test Title";
		var firstValue = new ValueCountPair<string?>("Value1", 1);
		var secondValue = new ValueCountPair<string?>("Value2", 2);
		var values = new List<ValueCountPair<string?>> { firstValue, secondValue };
		var fileProperty = new FileProperty(title, values);

		// Act
		var representativeValue = fileProperty.RepresentativeValue;

		// Assert
		Assert.Equal(firstValue, representativeValue);
	}

	/// <summary>
	/// Values が空の場合に RepresentativeValue にアクセスすると InvalidOperationException がスローされることを確認するテスト
	/// </summary>
	[Fact]
	public void RepresentativeValue_ThrowsInvalidOperationException_WhenValuesEmpty() {
		// Arrange
		var title = "Test Title";
		var values = new List<ValueCountPair<string?>>();
		var fileProperty = new FileProperty(title, values);

		// Act & Assert
		Assert.Throws<InvalidOperationException>(() => fileProperty.RepresentativeValue);
	}

	/// <summary>
	/// HasMultipleValues が期待される値を返すことを確認するテスト
	/// </summary>
	[Theory]
	[InlineData(0, false)]
	[InlineData(1, false)]
	[InlineData(2, true)]
	[InlineData(3, true)]
	public void HasMultipleValues_ReturnsExpectedValue(int count, bool expected) {
		// Arrange
		var title = "Test Title";
		var values = new List<ValueCountPair<string?>>();
		for (int i = 0; i < count; i++) {
			values.Add(new ValueCountPair<string?>($"Value{i}", 1));
		}
		var fileProperty = new FileProperty(title, values);

		// Act
		var hasMultipleValues = fileProperty.HasMultipleValues;

		// Assert
		Assert.Equal(expected, hasMultipleValues);
	}
}