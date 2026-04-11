using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Core.Models.Tags;
using MediaDeck.Database.Tables;

namespace MediaDeck.Core.Models.Tags;

/// <summary>
/// タグ関連モデルのファクトリ実装クラス
/// </summary>
[Inject(InjectServiceLifetime.Singleton, typeof(ITagModelFactory))]
public class TagModelFactory(
	Func<ITagModel> tagModelFactory,
	Func<ITagCategoryModel> tagCategoryModelFactory,
	Func<ITagAliasModel> tagAliasModelFactory)
	: ITagModelFactory {
	private readonly Func<ITagModel> _tagModelFactory = tagModelFactory;
	private readonly Func<ITagCategoryModel> _tagCategoryModelFactory = tagCategoryModelFactory;
	private readonly Func<ITagAliasModel> _tagAliasModelFactory = tagAliasModelFactory;

	/// <summary>
	/// タグモデルを作成します。
	/// </summary>
	public ITagModel Create(Tag tag, ITagCategoryModel? category = null) {
		var model = this._tagModelFactory();
		model.Initialize(tag, category, this);
		return model;
	}

	/// <summary>
	/// タグカテゴリーモデルを作成します。
	/// </summary>
	public ITagCategoryModel CreateCategory(TagCategory tagCategory) {
		var model = this._tagCategoryModelFactory();
		model.Initialize(tagCategory, this);
		return model;
	}

	/// <summary>
	/// 新しいタグカテゴリーモデルを作成します。
	/// </summary>
	public ITagCategoryModel CreateCategory() {
		return this._tagCategoryModelFactory();
	}

	/// <summary>
	/// タグ別名モデルを作成します。
	/// </summary>
	public ITagAliasModel CreateAlias(TagAlias tagAlias) {
		var model = this._tagAliasModelFactory();
		model.Initialize(tagAlias);
		return model;
	}

	/// <summary>
	/// 新しいタグ別名モデルを作成します。
	/// </summary>
	public ITagAliasModel CreateAlias() {
		return this._tagAliasModelFactory();
	}
}