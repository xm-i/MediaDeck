using System.IO;

using MetadataExtractor;
using MetadataExtractor.Formats.Netpbm;

namespace MediaDeck.MediaItemTypes.Image.Utils.Formats;

/// <summary>
/// Netpbmメタデータ取得クラス
/// </summary>
public class Netpbm : ImageBase {
	/// <summary>
	/// コンストラクタ
	/// </summary>
	/// <param name="stream">画像ファイルストリーム</param>
	public Netpbm(Stream stream) : base(stream) {
		var d = NetpbmMetadataReader.ReadMetadata(stream);
		this.Width = d.GetUInt16(NetpbmHeaderDirectory.TagWidth);
		this.Height = d.GetUInt16(NetpbmHeaderDirectory.TagHeight);
	}
}