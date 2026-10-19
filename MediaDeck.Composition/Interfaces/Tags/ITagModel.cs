namespace MediaDeck.Composition.Interfaces.Tags;

/// <summary>
/// タグのモデルクラスのインターフェース
/// </summary>

public interface ITagModel {
	/// <summary>
	/// 変更フラグ
	/// </summary>
	public bool IsDirty {
		get;
		set;
	}
	/// <summary>
	/// タグID
	/// </summary>
	public int TagId {
		get;
		set;
	}

	/// <summary>
	/// タグカテゴリーID
	/// </summary>
	public int? TagCategoryId {
		get;
		set;
	}

	/// <summary>
	/// タグカテゴリー
	/// </summary>
	public ITagCategoryModel TagCategory {
		get;
		set;
	}

	/// <summary>
	/// 新規作成（未採番）のタグかどうかを取得します。
	/// </summary>
	public bool IsNew {
		get;
	}

	/// <summary>
	/// タグ名
	/// </summary>
	public string TagName {
		get;
		set;
	}

	/// <summary>
	/// タグの説明
	/// </summary>
	public string Detail {
		get;
		set;
	}

	/// <summary>
	/// ローマ字表記
	/// </summary>
	public string Romaji {
		get;
		set;
	}

	/// <summary>
	/// 読み仮名 (Ruby)
	/// </summary>
	public string? Ruby {
		get; set;
	}

	/// <summary>
	/// 使用回数
	/// </summary>
	public ReactiveProperty<int> UsageCount {
		get;
	}

	/// <summary>
	/// タグ別名のリスト
	/// </summary>
	public List<ITagAliasModel> TagAliases {
		get;
		set;
	}

	/// <summary>
	/// 既存のタグエンティティからモデルを初期化します。
	/// </summary>
	public void Initialize(MediaDeck.Composition.Tables.Tag tag, ITagCategoryModel category, ITagModelFactory factory);
}