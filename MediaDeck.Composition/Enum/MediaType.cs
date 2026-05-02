using System.ComponentModel;

namespace MediaDeck.Composition.Enum;

/// <summary>
/// メディアタイプ
/// </summary>
public enum MediaType {
	/// <summary>
	/// 画像
	/// </summary>
	[Description("Image")]
	Image = 0,

	/// <summary>
	/// 動画
	/// </summary>
	[Description("Video")]
	Video = 1,

	/// <summary>
	/// PDF
	/// </summary>
	[Description("Pdf")]
	Pdf = 2,

	/// <summary>
	/// Archive
	/// </summary>
	[Description("Archive")]
	Archive = 3,

	/// <summary>
	/// フォルダグループ
	/// </summary>
	[Description("FolderGroup")]
	FolderGroup = 4,

	/// <summary>
	/// Unknown
	/// </summary>
	[Description("Unknown")]
	Unknown = 99
}