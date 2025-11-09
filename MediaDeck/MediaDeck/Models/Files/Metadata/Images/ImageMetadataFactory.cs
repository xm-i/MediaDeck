using System.IO;

using MetadataExtractor.Util;

using MediaDeck.Models.Files.Metadata.Images.Formats;

namespace MediaDeck.Models.Files.Metadata.Images;
/// <summary>
/// 画像メタデータクラス
/// </summary>
public static class ImageMetadataFactory {
	/// <summary>
	/// コンストラクタ
	/// </summary>
	/// <param name="stream">画像ファイルストリーム</param>
	public static IImage Create(Stream stream) {
		return FileTypeDetector.DetectFileType(stream) switch {
			FileType.Jpeg => new Jpeg(stream),
			FileType.Tiff or FileType.Arw or FileType.Cr2 or FileType.Nef or FileType.Orf or FileType.Rw2 => throw new ArgumentException("Unsupported file format."),
			FileType.Psd => new Psd(stream),
			FileType.Png => new Png(stream),
			FileType.Bmp => new Bmp(stream),
			FileType.Gif => new Gif(stream),
			FileType.Ico => new Ico(stream),
			FileType.Pcx => new Pcx(stream),
			FileType.Riff => new Riff(stream),
			FileType.Raf => new Raf(stream),
			FileType.QuickTime => throw new ArgumentException("Unsupported file format."),
			FileType.Netpbm => new Netpbm(stream),
			FileType.Heif => new Heif(stream),
			FileType.Unknown => throw new NotImplementedException(),
			FileType.Wav => throw new NotImplementedException(),
			FileType.Avi => throw new NotImplementedException(),
			FileType.WebP => throw new NotImplementedException(),
			FileType.Crw => throw new NotImplementedException(),
			FileType.Crx => throw new NotImplementedException(),
			FileType.Eps => throw new NotImplementedException(),
			FileType.Tga => throw new NotImplementedException(),
			FileType.Mp3 => throw new NotImplementedException(),
			FileType.Mp4 => throw new NotImplementedException(),
			_ => throw new ArgumentOutOfRangeException(nameof(stream), "Unsupported file format.")
		};
	}
}
