using MediaDeck.Composition.Tables;
using MediaDeck.Core.Models.Tags;
using Shouldly;

namespace MediaDeck.Core.Tests.Models.Tags;

/// <summary>
/// <see cref="TagAliasModel"/> の単体テスト。
/// </summary>
public class TagAliasModelTests {
	[Fact]
	public void Constructor_SetsAllProperties() {
		var alias = new TagAlias { TagAliasId = 5, TagId = 3, Alias = "テスト", Ruby = "てすと" };
		using var model = new TagAliasModel(alias);

		model.TagAliasId.ShouldBe(5);
		model.TagId.ShouldBe(3);
		model.Alias.ShouldBe("テスト");
		model.Ruby.ShouldBe("てすと");
		model.Romaji.ShouldBe("tesuto");
	}

	[Fact]
	public void SetAlias_RecalculatesRomaji_WhenRubyIsEmpty() {
		using var model = new TagAliasModel {
			Alias = "サンプル"
		};

		model.Romaji.ShouldBe("sanpuru");
	}

	[Fact]
	public void SetRuby_RecalculatesRomaji() {
		using var model = new TagAliasModel {
			Alias = "漢字",
			Ruby = "かんじ"
		};

		model.Romaji.ShouldBe("kanji");

		model.Ruby = "べつめい";
		model.Romaji.ShouldBe("betsumei");
	}
}