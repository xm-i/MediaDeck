using System.Collections.Generic;
using System.IO;

using MetadataExtractor;
using MetadataExtractor.Formats.Bmp;

namespace MediaDeck.MediaItemTypes.Image.Utils.Formats;

/// <summary>
/// Bmpメタデータ取得クラス
/// </summary>
internal class Bmp : ImageBase {
	private readonly IReadOnlyList<MetadataExtractor.Directory> _reader;

	/// <summary>
	/// コンストラクタ
	/// </summary>
	/// <param name="stream">画像ファイルストリーム</param>
	internal Bmp(Stream stream) : base(stream) {
		this._reader = BmpMetadataReader.ReadMetadata(stream);
		var d = this._reader.First(x => x is BmpHeaderDirectory);
		this.Width = d.GetUInt16(BmpHeaderDirectory.TagImageWidth);
		this.Height = d.GetUInt16(BmpHeaderDirectory.TagImageHeight);
	}

	internal Database.Tables.Metadata.Bmp CreateMetadataRecord() {
		var metadata = new Database.Tables.Metadata.Bmp();

		var b = this._reader.FirstOrDefault(x => x is BmpHeaderDirectory);

		metadata.BitsPerPixel = this.GetInt(b, BmpHeaderDirectory.TagBitsPerPixel);
		metadata.Compression = this.GetInt(b, BmpHeaderDirectory.TagCompression);
		metadata.XPixelsPerMeter = this.GetInt(b, BmpHeaderDirectory.TagXPixelsPerMeter);
		metadata.YPixelsPerMeter = this.GetInt(b, BmpHeaderDirectory.TagYPixelsPerMeter);
		metadata.PaletteColorCount = this.GetInt(b, BmpHeaderDirectory.TagPaletteColourCount);
		metadata.ImportantColorCount = this.GetInt(b, BmpHeaderDirectory.TagImportantColourCount);
		return metadata;
	}
}