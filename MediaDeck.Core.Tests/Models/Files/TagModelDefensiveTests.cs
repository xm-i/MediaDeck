using MediaDeck.Core.Models.Tags;
using Shouldly;

namespace MediaDeck.Core.Tests.Models.Files;

/// <summary>
/// タグ関連モデルのデフォルトコンストラクタ初期化の安全性を検証する単体テスト。
/// </summary>
public class TagModelDefensiveTests {
	[Fact]
	public void TagModel_DefaultConstructor_ShouldInitializeSafeDefaults() {
		using var model = new TagModel();

		model.TagName.ShouldBe(string.Empty);
		model.Detail.ShouldBe(string.Empty);
		model.Romaji.ShouldBe(string.Empty);
		model.Ruby.ShouldBeNull();
		model.TagAliases.ShouldBeEmpty();
		model.TagCategory.ShouldNotBeNull();
		model.UsageCount.Value.ShouldBe(0);
	}

	[Fact]
	public void TagCategoryModel_DefaultConstructor_ShouldInitializeSafeDefaults() {
		using var model = new TagCategoryModel();

		model.TagCategoryId.ShouldBeNull();
		model.TagCategoryName.ShouldBe("未設定");
		model.Detail.ShouldBe("カテゴリーが設定されていないタグ");
		model.Tags.ShouldBeEmpty();
	}

	[Fact]
	public void TagAliasModel_DefaultConstructor_ShouldInitializeSafeDefaults() {
		using var model = new TagAliasModel();

		model.Alias.ShouldBe(string.Empty);
		model.Romaji.ShouldBe(string.Empty);
		model.Ruby.ShouldBeNull();
	}
}