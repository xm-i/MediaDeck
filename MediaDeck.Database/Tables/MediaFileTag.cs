namespace MediaDeck.Database.Tables; 
/// <summary>
/// メディアファイル・タグ中間テーブル
/// </summary>
public class MediaFileTag {

	/// <summary>
	/// メディアファイルID
	/// </summary>
	public long MediaFileId {
		get;
		set;
	}

	/// <summary>
	/// メディアファイル
	/// </summary>
	public MediaFile MediaFile {
		get {
			return field ?? throw new InvalidOperationException();
		}
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
	/// タグ
	/// </summary>
	public Tag Tag {
		get {
			return field ?? throw new InvalidOperationException();
		}
		set;
	}
}
