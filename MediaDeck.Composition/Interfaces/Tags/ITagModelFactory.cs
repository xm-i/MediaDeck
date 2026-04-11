using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Database.Tables;

namespace MediaDeck.Composition.Interfaces.Tags;

/// <summary>
/// タグ関連モデルのファクトリ・インターフェース
/// </summary>
public interface ITagModelFactory {
	/// <summary>
	/// タグモデルを作成します。
	/// </summary>
	public ITagModel Create(Tag tag, ITagCategoryModel? category = null);

	/// <summary>
	/// タグカテゴリーモデルを作成します。
	/// </summary>
	public ITagCategoryModel CreateCategory(TagCategory tagCategory);

	/// <summary>
	/// 新しいタグカテゴリーモデルを作成します。
	/// </summary>
	public ITagCategoryModel CreateCategory();

	/// <summary>
	/// タグ別名モデルを作成します。
	/// </summary>
	public ITagAliasModel CreateAlias(TagAlias tagAlias);

	/// <summary>
	/// 新しいタグ別名モデルを作成します。
	/// </summary>
	public ITagAliasModel CreateAlias();
}