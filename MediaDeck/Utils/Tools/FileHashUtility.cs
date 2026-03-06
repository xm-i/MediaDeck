using System.IO;
using System.Security.Cryptography;

namespace MediaDeck.Utils.Tools;

/// <summary>
/// ファイルハッシュ計算ユーティリティ
/// </summary>
internal static class FileHashUtility {
	/// <summary>
	/// ファイルのSHA256ハッシュを計算する
	/// </summary>
	/// <param name="filePath">ファイルパス</param>
	/// <returns>ハッシュ文字列（16進数）</returns>
	public static string ComputeFileHash(string filePath) {
		using var fs = File.OpenRead(filePath);
		var hashBytes = SHA256.HashData(fs);
		return Convert.ToHexString(hashBytes);
	}

	/// <summary>
	/// ストリームからSHA256ハッシュを計算する
	/// </summary>
	/// <param name="stream">ストリーム</param>
	/// <returns>ハッシュ文字列（16進数）</returns>
	public static string ComputeFileHash(Stream stream) {
		var position = stream.Position;
		var hashBytes = SHA256.HashData(stream);
		stream.Position = position;
		return Convert.ToHexString(hashBytes);
	}
}
