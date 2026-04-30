using MediaDeck.Composition.Tables.Metadata;

namespace MediaDeck.Composition.Tables;

/// <summary>
/// 動画ファイルテーブル
/// </summary>
public class VideoFile {
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