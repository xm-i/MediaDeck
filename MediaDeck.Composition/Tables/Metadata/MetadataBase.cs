namespace MediaDeck.Composition.Tables.Metadata;

/// <summary>
/// メタデータテーブル共通部
/// </summary>
public abstract class MetadataBase {
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
}