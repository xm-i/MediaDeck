using System.IO;

using MetadataExtractor;
using MetadataExtractor.Formats.WebP;

namespace MediaDeck.FileTypes.Image.Utils.Formats;

/// <summary>
/// Riffメタデータ取得クラス
/// </summary>
internal class Riff : ImageBase {
	/// <summary>
	/// コンストラクタ
	/// </summary>
	/// <param name="stream">画像ファイルストリーム</param>
	internal Riff(Stream stream) : base(stream) {
		var reader = WebPMetadataReader.ReadMetadata(stream);
		var d = reader.First(x => x is WebPDirectory);
		this.Width = d.GetUInt16(WebPDirectory.TagImageWidth);
		this.Height = d.GetUInt16(WebPDirectory.TagImageHeight);
	}
}