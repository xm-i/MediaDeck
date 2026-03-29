namespace MediaDeck.Database.Tables;

/// <summary>
/// 画像ファイルテーブル
/// </summary>
public class ImageFile {
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
	/// 画像の方向
	/// </summary>
	public int? Orientation {
		get;
		set;
	}
}