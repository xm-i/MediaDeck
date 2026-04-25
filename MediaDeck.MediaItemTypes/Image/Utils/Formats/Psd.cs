using System.IO;

using MetadataExtractor;
using MetadataExtractor.Formats.Photoshop;

namespace MediaDeck.MediaItemTypes.Image.Utils.Formats;

/// <summary>
/// Psdメタデータ取得クラス
/// </summary>
internal class Psd : ImageBase {
	/// <summary>
	/// コンストラクタ
	/// </summary>
	/// <param name="stream">画像ファイルストリーム</param>
	internal Psd(Stream stream) : base(stream) {
		var reader = PsdMetadataReader.ReadMetadata(stream);
		var d = reader.First(x => x is PsdHeaderDirectory);
		this.Width = d.GetUInt16(PsdHeaderDirectory.TagImageWidth);
		this.Height = d.GetUInt16(PsdHeaderDirectory.TagImageHeight);
	}
}