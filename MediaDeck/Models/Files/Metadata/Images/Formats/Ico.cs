using System.IO;

using MetadataExtractor;
using MetadataExtractor.Formats.Ico;

namespace MediaDeck.Models.Files.Metadata.Images.Formats; 
/// <summary>
/// Icoメタデータ取得クラス
/// </summary>
public class Ico : ImageBase {
	/// <summary>
	/// 幅
	/// </summary>
	public override int Width {
		get;
	}

	/// <summary>
	/// 高さ
	/// </summary>
	public override int Height {
		get;
	}

	/// <summary>
	/// コンストラクタ
	/// </summary>
	/// <param name="stream">画像ファイルストリーム</param>

	internal Ico(Stream stream) : base(stream) {
		var reader = IcoMetadataReader.ReadMetadata(stream);
		var d = reader.First(x => x is IcoDirectory);
		this.Width = d.GetUInt16(IcoDirectory.TagImageWidth);
		this.Height = d.GetUInt16(IcoDirectory.TagImageHeight);
	}
}
