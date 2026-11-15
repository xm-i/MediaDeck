namespace MediaDeck.Composition.Interfaces.Tags;

/// <summary>
/// タグのモデルクラスのインターフェース
/// </summary>
public interface ITagModel : IDisposable {
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
		get;
		set;
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
	/// プロパティ等の変更通知ストリーム
	/// </summary>
	public Observable<Unit> Changed {
		get;
	}

	/// <summary>
	/// 変更通知を発火します。
	/// </summary>
	public void NotifyChanged();
}