using MediaDeck.Database.Tables;
using R3.JsonConfig.Attributes;

namespace MediaDeck.Composition.Interfaces.Tags;

/// <summary>
/// タグカテゴリーのモデルクラスのインターフェース
/// </summary>
[GenerateR3JsonConfigDto]
public interface ITagCategoryModel {
	/// <summary>
	/// タグカテゴリーID
	/// </summary>
	public int? TagCategoryId {
		get;
		set;
	}

	/// <summary>
	/// タグカテゴリー名
	/// </summary>
	public string TagCategoryName {
		get;
		set;
	}

	/// <summary>
	/// タグカテゴリーの説明
	/// </summary>
	public string Detail {
		get;
		set;
	}

	/// <summary>
	/// カテゴリーに属するタグのリスト
	/// </summary>
	public ObservableList<ITagModel> Tags {
		get;
	}

	public void Initialize(TagCategory? tagCategory, ITagModelFactory factory);
}