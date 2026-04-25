namespace MediaDeck.Database.Tables;

/// <summary>
/// メディアアイテム種別
/// </summary>
public enum ItemType {
	/// <summary>
	/// 画像
	/// </summary>
	Image = 0,

	/// <summary>
	/// 動画
	/// </summary>
	Video = 1,

	/// <summary>
	/// PDF
	/// </summary>
	Pdf = 2,

	/// <summary>
	/// アーカイブ
	/// </summary>
	Archive = 3,

	/// <summary>
	/// フォルダグループ
	/// </summary>
	FolderGroup = 4,

	/// <summary>
	/// 不明
	/// </summary>
	Unknown = 99
}