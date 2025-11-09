namespace MediaDeck.Database.Tables;
/// <summary>
/// タグ別名テーブル
/// </summary>
public class TagAlias {
	private string? _alias;

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
		get {
			return this._alias ?? throw new InvalidOperationException();
		}
		set {
			this._alias = value;
		}
	}

	/// <summary>
	/// 読み仮名
	/// </summary>
	public string? Ruby {
		get;
		set;
	}

	/// <summary>
	/// 関連するタグ
	/// </summary>
	public virtual Tag Tag {
		get;
		set;
	} = null!;
}