namespace MediaDeck.Database.Tables;

/// <summary>
/// 画像ファイルテーブル
/// </summary>
public class ImageFile {
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
	/// 画像の方向
	/// </summary>
	public int? Orientation {
		get;
		set;
	}
}