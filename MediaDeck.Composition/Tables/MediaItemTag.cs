namespace MediaDeck.Composition.Tables;

/// <summary>
/// メディアアイテム・タグ中間テーブル
/// </summary>
public class MediaItemTag {
	/// <summary>
	/// メディアアイテムID
	/// </summary>
	public long MediaItemId {
		get;
		set;
	}

	/// <summary>
	/// メディアアイテム
	/// </summary>
	public MediaItem MediaItem {
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