using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Composition.Tables;
using MediaDeck.Core.Models.Tags;
using Moq;
using R3;
using Shouldly;

namespace MediaDeck.Core.Tests.Models.Tags;

/// <summary>
/// <see cref="TagModel"/> の単体テスト。
/// </summary>
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

		var model = new TagModel(tag, categoryMock.Object, factoryMock.Object);
		return (model, tag, categoryMock, factoryMock);
	}

	[Fact]
	public void Constructor_SetsAllProperties() {
		using var model = CreateInitializedModel().model;

		model.TagId.ShouldBe(1);
		model.TagCategoryId.ShouldBe(1);
		model.TagName.ShouldBe("TestTag");
		model.Detail.ShouldBe("TestDetail");
		model.Ruby.ShouldBe("てすとたぐ");
		model.Romaji.ShouldBe("tesutotagu");
	}

	[Fact]
	public void SetTagName_UpdatesRomaji() {
		using var model = CreateInitializedModel().model;
		model.Ruby = null;
		model.TagName = "カタカナ";

		model.TagName.ShouldBe("カタカナ");
		model.Romaji.ShouldBe("katakana");
	}

	[Fact]
	public void SetRuby_UpdatesRomaji() {
		using var model = CreateInitializedModel().model;
		model.Ruby = "あたらしい";

		model.Ruby.ShouldBe("あたらしい");
		model.Romaji.ShouldBe("atarashii");
	}

	[Fact]
	public void NotifyChanged_EmitsChangedObservable() {
		using var model = CreateInitializedModel().model;
		var notified = false;
		using var sub = model.Changed.Subscribe(_ => notified = true);

		model.NotifyChanged();

		notified.ShouldBeTrue();
	}

	[Fact]
	public void TagAliases_IsEmptyAfterInit() {
		using var model = CreateInitializedModel().model;
		model.TagAliases.Count.ShouldBe(0);
	}

	[Fact]
	public void UsageCount_DefaultIsZero() {
		using var model = CreateInitializedModel().model;
		model.UsageCount.Value.ShouldBe(0);
	}

	[Fact]
	public void SetTagCategory_UpdatesCategory() {
		using var model = CreateInitializedModel().model;
		var newCategoryMock = new Mock<ITagCategoryModel>();

		model.TagCategory = newCategoryMock.Object;
		model.TagCategory.ShouldBe(newCategoryMock.Object);
	}
}