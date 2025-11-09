namespace MediaDeck.Database.Tables; 
/// <summary>
/// タグテーブル
/// </summary>
public class Tag {
	private ICollection<MediaFileTag>? _mediaFileTags;
	private ICollection<TagAlias>? _tagAliases;

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
	public int TagCategoryId {
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
			return this._mediaFileTags ?? throw new InvalidOperationException();
		}
		set {
			this._mediaFileTags = value;
		}
	}

	/// <summary>
	/// タグの別名
	/// </summary>
	public virtual ICollection<TagAlias> TagAliases {
		get {
			return this._tagAliases ?? throw new InvalidOperationException();
		}
		set {
			this._tagAliases = value;
		}
	}

	/// <summary>
	/// タグ分類
	/// </summary>
	public virtual required TagCategory TagCategory {
		get;
		set;
	}
}
