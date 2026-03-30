using System.IO;
using System.Linq;

using MetadataExtractor;
using MetadataExtractor.Formats.Jpeg;
using MetadataExtractor.Formats.Raf;

namespace MediaDeck.FileTypes.Image.Utils.Formats;

/// <summary>
/// Rafメタデータ取得クラス
/// </summary>
internal class Raf : ImageBase {

	/// <summary>
	/// コンストラクタ
	/// </summary>
	/// <param name="stream">画像ファイルストリーム</param>
	internal Raf(Stream stream) : base(stream) {
		var reader = RafMetadataReader.ReadMetadata(stream);
		var d = reader.First(x => x is JpegDirectory);
		this.Width = d.GetUInt16(JpegDirectory.TagImageWidth);
		this.Height = d.GetUInt16(JpegDirectory.TagImageHeight);
	}
}