using MediaDeck.Database.Tables.Metadata;

namespace MediaDeck.Database.Tables;

/// <summary>
/// 動画ファイルテーブル
/// </summary>
public class VideoFile {
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
	/// 動画の長さ
	/// </summary>
	public double? Duration {
		get;
		set;
	}

	/// <summary>
	/// 回転
	/// </summary>
	public int? Rotation {
		get;
		set;
	}

	public virtual ICollection<VideoMetadataValue> VideoMetadataValues {
		get {
			return field ?? throw new InvalidOperationException();
		}
		set;
	}
}