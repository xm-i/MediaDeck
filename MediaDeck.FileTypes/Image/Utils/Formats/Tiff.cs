using System.IO;

namespace MediaDeck.FileTypes.Image.Utils.Formats;

/// <summary>
/// Tiffメタデータ取得クラス
/// </summary>
internal class Tiff : ImageBase {

	/// <summary>
	/// コンストラクタ
	/// </summary>
	/// <param name="stream">画像ファイルストリーム</param>
	internal Tiff(Stream stream) : base(stream) { }
}