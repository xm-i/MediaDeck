using MediaDeck.Composition.Tables;

namespace MediaDeck.Composition.Interfaces.Tags;

/// <summary>
/// タグ関連モデルのファクトリ・インターフェース
/// </summary>
public interface ITagModelFactory {
	/// <summary>
	/// タグモデルを作成します。
	/// </summary>
	/// <param name="tag">タグエンティティ</param>
	/// <param name="category">所属カテゴリモデル</param>
	/// <returns>作成されたタグモデル</returns>
	public ITagModel Create(Tag tag, ITagCategoryModel category);

	/// <summary>
	/// タグカテゴリーモデルを作成します。
	/// </summary>
	/// <param name="tagCategory">タグカテゴリーエンティティ（未分類の場合は null）</param>
	/// <returns>作成されたタグカテゴリーモデル</returns>
	public ITagCategoryModel CreateCategory(TagCategory? tagCategory);

	/// <summary>
	/// 空の新しいタグカテゴリーモデルを作成します。
	/// </summary>
	/// <returns>作成されたタグカテゴリーモデル</returns>
	public ITagCategoryModel CreateCategory();

	/// <summary>
	/// タグ別名モデルを作成します。
	/// </summary>
	/// <param name="tagAlias">タグ別名エンティティ</param>
	/// <returns>作成されたタグ別名モデル</returns>
	public ITagAliasModel CreateAlias(TagAlias tagAlias);

	/// <summary>
	/// 空の新しいタグ別名モデルを作成します。
	/// </summary>
	/// <returns>作成されたタグ別名モデル</returns>
	public ITagAliasModel CreateAlias();
}