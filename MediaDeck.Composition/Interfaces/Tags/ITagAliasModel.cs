namespace MediaDeck.Composition.Interfaces.Tags;

/// <summary>
/// タグ別名のモデルクラスのインターフェース
/// </summary>
public interface ITagAliasModel : IDisposable {
	/// <summary>
	/// タグ別名ID
	/// </summary>
	public int TagAliasId {
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
	/// 別名
	/// </summary>
	public string Alias {
		get;
		set;
	}

	/// <summary>
	/// 読み仮名
	/// </summary>
	public string? Ruby {
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
}