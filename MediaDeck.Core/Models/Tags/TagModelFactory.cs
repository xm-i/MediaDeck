using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Core.Models.Tags;
using MediaDeck.Database.Tables;
using Microsoft.Extensions.DependencyInjection;

namespace MediaDeck.Core.Models.Tags;

/// <summary>
/// タグ関連モデルのファクトリ実装クラス
/// </summary>
[Inject(InjectServiceLifetime.Singleton, typeof(ITagModelFactory))]
public class TagModelFactory(IServiceProvider serviceProvider)
	: ITagModelFactory {
	private readonly IServiceProvider _serviceProvider = serviceProvider;

	/// <summary>
	/// タグモデルを作成します。
	/// </summary>
	public ITagModel Create(Tag tag, ITagCategoryModel? category = null) {
		var model = this._serviceProvider.GetRequiredService<ITagModel>();
		model.Initialize(tag, category, this);
		return model;
	}

	/// <summary>
	/// タグカテゴリーモデルを作成します。
	/// </summary>
	public ITagCategoryModel CreateCategory(TagCategory tagCategory) {
		var model = this._serviceProvider.GetRequiredService<ITagCategoryModel>();
		model.Initialize(tagCategory, this);
		return model;
	}

	/// <summary>
	/// 新しいタグカテゴリーモデルを作成します。
	/// </summary>
	public ITagCategoryModel CreateCategory() {
		return this._serviceProvider.GetRequiredService<ITagCategoryModel>();
	}

	/// <summary>
	/// タグ別名モデルを作成します。
	/// </summary>
	public ITagAliasModel CreateAlias(TagAlias tagAlias) {
		var model = this._serviceProvider.GetRequiredService<ITagAliasModel>();
		model.Initialize(tagAlias);
		return model;
	}

	/// <summary>
	/// 新しいタグ別名モデルを作成します。
	/// </summary>
	public ITagAliasModel CreateAlias() {
		return this._serviceProvider.GetRequiredService<ITagAliasModel>();
	}
}