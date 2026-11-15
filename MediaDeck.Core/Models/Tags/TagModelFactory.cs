using MediaDeck.Composition.Interfaces;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Composition.Tables;

namespace MediaDeck.Core.Models.Tags;

/// <summary>
/// タグ関連モデルのファクトリ実装クラス
/// </summary>
[Inject(InjectServiceLifetime.Singleton, typeof(ITagModelFactory))]
public class TagModelFactory(IStringProvider? stringProvider = null) : ITagModelFactory {
	/// <inheritdoc />
	public ITagModel Create(Tag tag, ITagCategoryModel category) {
		return new TagModel(tag, category, this);
	}

	/// <inheritdoc />
	public ITagCategoryModel CreateCategory(TagCategory? tagCategory) {
		return new TagCategoryModel(tagCategory, this, stringProvider);
	}

	/// <inheritdoc />
	public ITagCategoryModel CreateCategory() {
		return new TagCategoryModel(null, this, stringProvider) {
			TagCategoryName = string.Empty,
			Detail = string.Empty
		};
	}

	/// <inheritdoc />
	public ITagAliasModel CreateAlias(TagAlias tagAlias) {
		return new TagAliasModel(tagAlias);
	}

	/// <inheritdoc />
	public ITagAliasModel CreateAlias() {
		return new TagAliasModel(new TagAlias {
			Alias = string.Empty,
			Ruby = null
		});
	}
}