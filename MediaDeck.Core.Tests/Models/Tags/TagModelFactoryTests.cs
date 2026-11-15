using MediaDeck.Composition.Interfaces;
using MediaDeck.Composition.Tables;
using MediaDeck.Core.Models.Tags;
using Moq;
using Shouldly;

namespace MediaDeck.Core.Tests.Models.Tags;

/// <summary>
/// <see cref="TagModelFactory"/> の単体テスト。
/// </summary>
public class TagModelFactoryTests {
	private static TagModelFactory CreateSut() {
		var spMock = new Mock<IStringProvider>();
		spMock.Setup(x => x.GetString("TagCategory_Unassigned_Name")).Returns("Unassigned");
		spMock.Setup(x => x.GetString("TagCategory_Unassigned_Detail")).Returns("Unassigned Detail");
		return new TagModelFactory(spMock.Object);
	}

	[Fact]
	public void Create_CreatesInitializedTagModel() {
		var factory = CreateSut();
		using var category = factory.CreateCategory();
		var tag = new Tag {
			TagId = 1,
			TagName = "Test",
			Ruby = "てすと",
			TagCategoryId = null,
			TagCategory = null!,
			MediaItemTags = [],
			TagAliases = [new TagAlias { TagId = 1, TagAliasId = 0, Alias = "エイリアス", Ruby = "えいりあす" }],
			Detail = "Detail"
		};

		using var result = factory.Create(tag, category);

		result.TagId.ShouldBe(1);
		result.TagName.ShouldBe("Test");
		result.Detail.ShouldBe("Detail");
		result.TagCategory.ShouldBe(category);
		result.TagAliases.Count.ShouldBe(1);
		result.TagAliases[0].Alias.ShouldBe("エイリアス");
	}

	[Fact]
	public void CreateCategory_WithTagCategory_CreatesInitializedCategoryModel() {
		var factory = CreateSut();
		var tagCategory = new TagCategory { TagCategoryId = 1, TagCategoryName = "Cat", Detail = "CatDetail", Tags = [] };

		using var result = factory.CreateCategory(tagCategory);

		result.TagCategoryId.ShouldBe(1);
		result.TagCategoryName.ShouldBe("Cat");
		result.Detail.ShouldBe("CatDetail");
	}

	[Fact]
	public void CreateCategory_WithNull_UsesStringProvider() {
		var factory = CreateSut();

		using var result = factory.CreateCategory(null);

		result.TagCategoryId.ShouldBeNull();
		result.TagCategoryName.ShouldBe("Unassigned");
		result.Detail.ShouldBe("Unassigned Detail");
	}

	[Fact]
	public void CreateCategory_NoArgs_CreatesEmptyCategoryModel() {
		var factory = CreateSut();

		using var result = factory.CreateCategory();

		result.TagCategoryId.ShouldBeNull();
		result.TagCategoryName.ShouldBe(string.Empty);
		result.Detail.ShouldBe(string.Empty);
	}

	[Fact]
	public void CreateAlias_WithTagAlias_CreatesInitializedAliasModel() {
		var factory = CreateSut();
		var alias = new TagAlias { TagAliasId = 1, TagId = 2, Alias = "A1", Ruby = "あいち" };

		using var result = factory.CreateAlias(alias);

		result.TagAliasId.ShouldBe(1);
		result.TagId.ShouldBe(2);
		result.Alias.ShouldBe("A1");
		result.Ruby.ShouldBe("あいち");
		result.Romaji.ShouldBe("aichi");
	}

	[Fact]
	public void CreateAlias_NoArgs_CreatesEmptyAliasModel() {
		var factory = CreateSut();

		using var result = factory.CreateAlias();

		result.Alias.ShouldBe(string.Empty);
		result.Ruby.ShouldBeNull();
		result.Romaji.ShouldBe(string.Empty);
	}
}