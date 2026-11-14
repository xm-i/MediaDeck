using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Composition.Tables;
using MediaDeck.Core.Models.Tags;
using Moq;
using R3;
using Shouldly;

namespace MediaDeck.Core.Tests.Models.Tags;

/// <summary>
/// <see cref="TagCategoryModel"/> の単体テスト。
/// </summary>
public class TagCategoryModelTests {
	[Fact]
	public void Constructor_WithNull_SetsDefaultCategoryName() {
		var factoryMock = new Mock<ITagModelFactory>();
		using var model = new TagCategoryModel(null, factoryMock.Object);

		model.TagCategoryId.ShouldBeNull();
		model.TagCategoryName.ShouldBe("未設定");
		model.Detail.ShouldBe("カテゴリーが設定されていないタグ");
		model.Tags.Count.ShouldBe(0);
	}

	[Fact]
	public void Constructor_WithTagCategory_SetsProperties() {
		var factoryMock = new Mock<ITagModelFactory>();
		var tagCategory = new TagCategory {
			TagCategoryId = 1,
			TagCategoryName = "Cat1",
			Detail = "Detail1",
			Tags = []
		};

		using var model = new TagCategoryModel(tagCategory, factoryMock.Object);

		model.TagCategoryId.ShouldBe(1);
		model.TagCategoryName.ShouldBe("Cat1");
		model.Detail.ShouldBe("Detail1");
	}

	[Fact]
	public void AddTag_AddsToCollection() {
		using var model = new TagCategoryModel();

		var tagMock = new Mock<ITagModel>();
		model.AddTag(tagMock.Object);

		model.Tags.Count.ShouldBe(1);
	}

	[Fact]
	public void RemoveTag_RemovesFromCollection() {
		using var model = new TagCategoryModel();

		var tagMock = new Mock<ITagModel>();
		model.AddTag(tagMock.Object);
		model.RemoveTag(tagMock.Object);

		model.Tags.Count.ShouldBe(0);
	}

	[Fact]
	public void ClearTags_ClearsCollection() {
		using var model = new TagCategoryModel();

		model.AddTag(new Mock<ITagModel>().Object);
		model.AddTag(new Mock<ITagModel>().Object);
		model.ClearTags();

		model.Tags.Count.ShouldBe(0);
	}

	[Fact]
	public void SetTagCategoryName_EmitsChanged() {
		using var model = new TagCategoryModel();
		var notified = false;
		using var sub = model.Changed.Subscribe(_ => notified = true);

		model.TagCategoryName = "NewName";

		notified.ShouldBeTrue();
	}

	[Fact]
	public void SetDetail_EmitsChanged() {
		using var model = new TagCategoryModel();
		var notified = false;
		using var sub = model.Changed.Subscribe(_ => notified = true);

		model.Detail = "NewDetail";

		notified.ShouldBeTrue();
	}
}