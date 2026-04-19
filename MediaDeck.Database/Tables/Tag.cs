namespace MediaDeck.Database.Tables;

/// <summary>
/// タグテーブル
/// </summary>
public class Tag {
	/// <summary>
	/// タグID
	/// </summary>
	public int TagId {
		get;
		set;
	}

	/// <summary>
	/// タグ分類
	/// </summary>
	public int? TagCategoryId {
		get;
		set;
	}

	/// <summary>
	/// タグ名
	/// </summary>
	public required string TagName {
		get;
		set;
	}

	/// <summary>
	/// 読み仮名 (Ruby)
	/// </summary>
	public string? Ruby {
		get;
		set;
	}

	/// <summary>
	/// タグ説明
	/// </summary>
	public required string Detail {
		get;
		set;
	}

	/// <summary>
	/// タグをつけているメディアファイル
	/// </summary>
	public virtual ICollection<MediaFileTag> MediaFileTags {
		get {
			return field ?? throw new InvalidOperationException();
		}
		set;
	}

	/// <summary>
	/// タグの別名
	/// </summary>
	public virtual ICollection<TagAlias> TagAliases {
		get {
			return field ?? throw new InvalidOperationException();
		}
		set;
	}

	/// <summary>
	/// タグ分類
	/// </summary>
	public virtual TagCategory? TagCategory {
		get;
		set;
	}
}