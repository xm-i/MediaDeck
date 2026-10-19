using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Core.Models.Tags;
using Moq;
using Shouldly;

namespace MediaDeck.Core.Tests.Models.Tags;

/// <summary>
/// <see cref="TagExtensions"/> の単体テスト。
/// </summary>
public class TagExtensionsTests {
	[Theory]
	[InlineData("test", true, null, "testtag")] // タグ名一致
	[InlineData("alias", true, "TestAlias", "tag")] // エイリアス一致
	[InlineData("ruby", true, "TestAlias", "tag")] // ルビ一致
	[InlineData("romaji", true, "TestAlias", "tag")] // ローマ字一致
	[InlineData("nomatch", false, null, "tag")] // 不一致
	[InlineData("", false, null, "tag")] // 空文字
	[InlineData(null, false, null, "tag")] // null
	public void MatchesFilter_ShouldFilterCorrectly(string? searchText, bool expectedResult, string? expectedRepresentativeText, string tagName) {
		// Arrange
		var tagMock = new Mock<ITagModel>();
		tagMock.SetupGet(x => x.TagName).Returns(tagName);

		var aliasList = new List<ITagAliasModel>();
		if (tagName == "tag") {
			var aliasMock = new Mock<ITagAliasModel>();
			aliasMock.SetupGet(x => x.Alias).Returns("TestAlias");
			aliasMock.SetupGet(x => x.Ruby).Returns("testruby");
			aliasMock.SetupGet(x => x.Romaji).Returns("testromaji");
			aliasList.Add(aliasMock.Object);
		}
		tagMock.SetupGet(x => x.TagAliases).Returns(aliasList);

		// Act
		var result = tagMock.Object.MatchesFilter(searchText ?? string.Empty, out var representativeText);

		// Assert
		result.ShouldBe(expectedResult);
		representativeText.ShouldBe(expectedRepresentativeText);
	}
}