namespace MediaDeck.Database.Tables; 
/// <summary>
/// タグ分類テーブル
/// </summary>
public class TagCategory {

	/// <summary>
	/// タグ分類ID
	/// </summary>
	public int TagCategoryId {
		get;
		set;
	}

	/// <summary>
	/// タグ分類名
	/// </summary>
	public required string TagCategoryName {
		get;
		set;
	}

	/// <summary>
	/// タグ分類の説明
	/// </summary>
	public required string Detail {
		get;
		set;
	}

	/// <summary>
	/// タグ
	/// </summary>
	public virtual required ICollection<Tag> Tags {
		get;
		set;
	}
}
