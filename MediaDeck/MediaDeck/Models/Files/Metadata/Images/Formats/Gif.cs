using System.Collections.Generic;
using System.IO;

using MetadataExtractor;
using MetadataExtractor.Formats.Gif;

namespace MediaDeck.Models.Files.Metadata.Images.Formats;
/// <summary>
/// Gifメタデータ取得クラス
/// </summary>
public class Gif : ImageBase {
	private readonly IReadOnlyList<MetadataExtractor.Directory> _reader;
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
	internal Gif(Stream stream) : base(stream) {
		this._reader = GifMetadataReader.ReadMetadata(stream);
		var d = this._reader.First(x => x is GifHeaderDirectory);
		this.Width = d.GetUInt16(GifHeaderDirectory.TagImageWidth);
		this.Height = d.GetUInt16(GifHeaderDirectory.TagImageHeight);
	}

	public Database.Tables.Metadata.Gif CreateMetadataRecord() {
		var metadata = new Database.Tables.Metadata.Gif();

		var h = this._reader.FirstOrDefault(x => x is GifHeaderDirectory);

		metadata.ColorTableSize = this.GetInt(h, GifHeaderDirectory.TagColorTableSize);
		metadata.IsColorTableSorted = this.GetInt(h, GifHeaderDirectory.TagIsColorTableSorted);
		metadata.BitsPerPixel = this.GetInt(h, GifHeaderDirectory.TagBitsPerPixel);
		metadata.HasGlobalColorTable = this.GetInt(h, GifHeaderDirectory.TagHasGlobalColorTable);
		metadata.BackgroundColorIndex = this.GetInt(h, GifHeaderDirectory.TagBackgroundColorIndex);
		metadata.PixelAspectRatio = this.GetInt(h, GifHeaderDirectory.TagPixelAspectRatio);

		return metadata;
	}
}
