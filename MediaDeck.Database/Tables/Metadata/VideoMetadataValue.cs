namespace MediaDeck.Database.Tables.Metadata; 
public class VideoMetadataValue {


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
	public VideoFile VideoFile {
		get {
			return field ?? throw new InvalidOperationException();
		}
		set;
	}

	/// <summary>
	/// キー
	/// </summary>
	public string Key {
		get {
			return field ?? throw new InvalidOperationException();
		}
		set;
	}

	/// <summary>
	/// 値
	/// </summary>
	public string Value {
		get {
			return field ?? throw new InvalidOperationException();
		}
		set;
	}
}
