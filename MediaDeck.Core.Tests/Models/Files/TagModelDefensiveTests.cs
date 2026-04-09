using System;
using MediaDeck.Core.Models.Files;
using Shouldly;
using Xunit;

namespace MediaDeck.Core.Tests.Models.Files;

public class TagModelDefensiveTests {
	[Fact]
	public void TagModel_Properties_ShouldThrow_WhenNotInitialized() {
		// Arrange
		var model = new TagModel();

		// Act & Assert
		Should.Throw<InvalidOperationException>(() => _ = model.TagName).Message.ShouldContain("TagName is not initialized.");
		Should.Throw<InvalidOperationException>(() => _ = model.TagCategory).Message.ShouldContain("TagCategory is not initialized.");
		Should.Throw<InvalidOperationException>(() => _ = model.TagAliases).Message.ShouldContain("TagAliases is not initialized.");
		Should.Throw<InvalidOperationException>(() => _ = model.TagId).Message.ShouldContain("TagId is not initialized.");
	}

	[Fact]
	public void TagCategoryModel_Properties_ShouldThrow_WhenNotInitialized() {
		// Arrange
		var model = new TagCategoryModel();

		// Act & Assert
		Should.Throw<InvalidOperationException>(() => _ = model.TagCategoryName).Message.ShouldContain("TagCategoryName is not initialized.");
		Should.Throw<InvalidOperationException>(() => _ = model.Detail).Message.ShouldContain("Detail is not initialized.");
	}

	[Fact]
	public void TagAliasModel_Properties_ShouldThrow_WhenNotInitialized() {
		// Arrange
		var model = new TagAliasModel();

		// Act & Assert
		Should.Throw<InvalidOperationException>(() => _ = model.Alias).Message.ShouldContain("Alias is not initialized.");
		Should.Throw<InvalidOperationException>(() => _ = model.Romaji).Message.ShouldContain("Romaji is not initialized.");
	}

	[Fact]
	public void TagAliasModel_Ruby_ShouldNotThrow_WhenNull() {
		// Arrange
		var model = new TagAliasModel();

		// Act
		var ruby = model.Ruby;

		// Assert
		ruby.ShouldBeNull(); // Ruby is optional, so it shouldn't throw.
	}
}