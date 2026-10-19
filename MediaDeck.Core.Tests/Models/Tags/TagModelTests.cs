using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Composition.Tables;
using MediaDeck.Core.Models.Tags;
using Moq;
using Shouldly;

namespace MediaDeck.Core.Tests.Models.Tags;

public class TagModelTests {
	private static (TagModel model, Tag tag, Mock<ITagCategoryModel> categoryMock, Mock<ITagModelFactory> factoryMock) CreateInitializedModel() {
		var tag = new Tag {
			TagId = 1,
			TagCategoryId = 1,
			TagName = "TestTag",
			Detail = "TestDetail",
			Ruby = "てすとたぐ",
			TagCategory = null!,
			MediaItemTags = [],
			TagAliases = []
		};
		var categoryMock = new Mock<ITagCategoryModel>();
		var factoryMock = new Mock<ITagModelFactory>();

		var model = new TagModel();
		model.Initialize(tag, categoryMock.Object, factoryMock.Object);
		return (model, tag, categoryMock, factoryMock);
	}

	[Fact]
	public void Initialize_SetsAllProperties() {
		var (model, _, _, _) = CreateInitializedModel();

		model.TagId.ShouldBe(1);
		model.TagCategoryId.ShouldBe(1);
		model.TagName.ShouldBe("TestTag");
		model.Detail.ShouldBe("TestDetail");
		model.Ruby.ShouldBe("てすとたぐ");
		model.Romaji.ShouldNotBeNullOrEmpty();
		model.IsDirty.ShouldBeFalse();
	}

	[Fact]
	public void Properties_BeforeInitialize_ThrowInvalidOperationException() {
		var model = new TagModel();
		Should.Throw<InvalidOperationException>(() => _ = model.TagId);
		Should.Throw<InvalidOperationException>(() => _ = model.TagCategoryId);
		Should.Throw<InvalidOperationException>(() => _ = model.TagName);
		Should.Throw<InvalidOperationException>(() => _ = model.Detail);
	}

	[Fact]
	public void SetTagName_SetsIsDirty() {
		var (model, _, _, _) = CreateInitializedModel();

		model.TagName = "NewName";
		model.IsDirty.ShouldBeTrue();
		model.TagName.ShouldBe("NewName");
	}

	[Fact]
	public void SetTagCategoryId_SetsIsDirty() {
		var (model, _, _, _) = CreateInitializedModel();

		model.TagCategoryId = 99;
		model.IsDirty.ShouldBeTrue();
	}

	[Fact]
	public void TagAliases_IsEmptyAfterInit() {
		var (model, _, _, _) = CreateInitializedModel();
		model.TagAliases.Count.ShouldBe(0);
	}

	[Fact]
	public void UsageCount_DefaultIsZero() {
		var (model, _, _, _) = CreateInitializedModel();
		model.UsageCount.Value.ShouldBe(0);
	}

	[Fact]
	public void IsNew_ShouldBeTrue_WhenNotInitialized() {
		var model = new TagModel();
		model.IsNew.ShouldBeTrue();
	}

	[Fact]
	public void IsNew_ShouldBeFalse_WhenInitializedWithTag() {
		var (model, _, _, _) = CreateInitializedModel();
		model.IsNew.ShouldBeFalse();
	}

	[Fact]
	public void SetTagCategory_SetsIsDirty() {
		var (model, _, _, _) = CreateInitializedModel();
		var newCategoryMock = new Mock<ITagCategoryModel>();

		model.TagCategory = newCategoryMock.Object;
		model.IsDirty.ShouldBeTrue();
		model.TagCategory.ShouldBe(newCategoryMock.Object);
	}
}