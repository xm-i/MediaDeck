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
	Image,
	/// <summary>
	/// 動画
	/// </summary>
	[Description("Video")]
	Video,
	/// <summary>
	/// PDF
	/// </summary>
	[Description("Pdf")]
	Pdf,
	/// <summary>
	/// Archive
	/// </summary>
	[Description("Archive")]
	Archive
}
