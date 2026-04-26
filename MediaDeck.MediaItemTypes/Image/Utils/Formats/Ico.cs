using System.IO;

using MetadataExtractor;
using MetadataExtractor.Formats.Ico;

namespace MediaDeck.MediaItemTypes.Image.Utils.Formats;

/// <summary>
/// Icoメタデータ取得クラス
/// </summary>
public class Ico : ImageBase {
	/// <summary>
	/// コンストラクタ
	/// </summary>
	/// <param name="stream">画像ファイルストリーム</param>
	public Ico(Stream stream) : base(stream) {
		var reader = IcoMetadataReader.ReadMetadata(stream);
		var d = reader.First(x => x is IcoDirectory);
		this.Width = d.GetUInt16(IcoDirectory.TagImageWidth);
		this.Height = d.GetUInt16(IcoDirectory.TagImageHeight);
	}
}