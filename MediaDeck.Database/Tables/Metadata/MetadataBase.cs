namespace MediaDeck.Database.Tables.Metadata;
/// <summary>
/// メタデータテーブル共通部
/// </summary>
public abstract class MetadataBase {


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
}
