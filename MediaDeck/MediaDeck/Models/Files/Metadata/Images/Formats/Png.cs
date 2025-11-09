using System.Collections.Generic;
using System.IO;

using MetadataExtractor;
using MetadataExtractor.Formats.Png;

namespace MediaDeck.Models.Files.Metadata.Images.Formats;
/// <summary>
/// Pngメタデータ取得クラス
/// </summary>
public class Png : ImageBase {
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
	internal Png(Stream stream) : base(stream) {
		this._reader = PngMetadataReader.ReadMetadata(stream);
		var d = this._reader.First(x => x is PngDirectory);
		this.Width = d.GetUInt16(PngDirectory.TagImageWidth);
		this.Height = d.GetUInt16(PngDirectory.TagImageHeight);
	}

	public Database.Tables.Metadata.Png CreateMetadataRecord() {
		var metadata = new Database.Tables.Metadata.Png();

		var p = this._reader.FirstOrDefault(x => x is PngDirectory);
		var pc = this._reader.FirstOrDefault(x => x is PngChromaticitiesDirectory);

		metadata.BitsPerSample = this.GetInt(p, PngDirectory.TagBitsPerSample);
		metadata.ColorType = this.GetInt(p, PngDirectory.TagColorType);
		metadata.CompressionType = this.GetInt(p, PngDirectory.TagCompressionType);
		metadata.FilterMethod = this.GetInt(p, PngDirectory.TagFilterMethod);
		metadata.InterlaceMethod = this.GetInt(p, PngDirectory.TagInterlaceMethod);
		metadata.PaletteSize = this.GetInt(p, PngDirectory.TagPaletteSize);
		metadata.PaletteHasTransparency = this.GetInt(p, PngDirectory.TagPaletteHasTransparency);
		metadata.SrgbRenderingIntent = this.GetInt(p, PngDirectory.TagSrgbRenderingIntent);
		metadata.Gamma = this.GetDouble(p, PngDirectory.TagGamma);
		metadata.IccProfileName = this.GetString(p, PngDirectory.TagIccProfileName);
		metadata.LastModificationTime = this.GetDateTime(p, PngDirectory.TagLastModificationTime);
		metadata.BackgroundColor = this.GetBinary(p, PngDirectory.TagBackgroundColor);
		metadata.PixelsPerUnitX = this.GetInt(p, PngDirectory.TagPixelsPerUnitX);
		metadata.PixelsPerUnitY = this.GetInt(p, PngDirectory.TagPixelsPerUnitY);
		metadata.UnitSpecifier = this.GetInt(p, PngDirectory.TagUnitSpecifier);
		metadata.SignificantBits = this.GetInt(p, PngDirectory.TagSignificantBits);

		metadata.WhitePointX = this.GetInt(pc, PngChromaticitiesDirectory.TagWhitePointX);
		metadata.WhitePointY = this.GetInt(pc, PngChromaticitiesDirectory.TagWhitePointY);
		metadata.RedX = this.GetInt(pc, PngChromaticitiesDirectory.TagRedX);
		metadata.RedY = this.GetInt(pc, PngChromaticitiesDirectory.TagRedY);
		metadata.GreenX = this.GetInt(pc, PngChromaticitiesDirectory.TagGreenX);
		metadata.GreenY = this.GetInt(pc, PngChromaticitiesDirectory.TagGreenY);
		metadata.BlueX = this.GetInt(pc, PngChromaticitiesDirectory.TagBlueX);
		metadata.BlueY = this.GetInt(pc, PngChromaticitiesDirectory.TagBlueY);

		return metadata;
	}
}
