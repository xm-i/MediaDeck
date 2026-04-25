namespace MediaDeck.Database.Tables.Metadata;

public class VideoMetadataValue {
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