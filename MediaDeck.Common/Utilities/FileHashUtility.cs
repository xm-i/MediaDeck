using System.Buffers;
using System.Security.Cryptography;

namespace MediaDeck.Common.Utilities;

/// <summary>
/// ファイルハッシュ計算ユーティリティ
/// </summary>
public static class FileHashUtility {
	/// <summary>
	/// ハッシュ計算に使用する最大バイト数 (1MB)
	/// </summary>
	private const int MaxBytesToHash = 1024 * 1024;

	/// <summary>
	/// ファイルの先頭1MBからSHA256ハッシュを計算する
	/// </summary>
	/// <param name="filePath">ファイルパス</param>
	/// <returns>ハッシュ文字列（16進数）</returns>
	public static string ComputeFileHash(string filePath) {
		using var fs = File.OpenRead(filePath);
		return ComputeFileHashCore(fs, fs.Length);
	}

	/// <summary>
	/// ストリームの先頭1MBからSHA256ハッシュを計算する
	/// </summary>
	/// <param name="stream">ストリーム</param>
	/// <returns>ハッシュ文字列（16進数）</returns>
	public static string ComputeFileHash(Stream stream) {
		var position = stream.Position;
		var result = ComputeFileHashCore(stream, stream.Length);
		stream.Position = position;
		return result;
	}

	private static string ComputeFileHashCore(Stream stream, long fileSize) {
		var bytesToRead = (int)Math.Min(fileSize, MaxBytesToHash);
		var buffer = ArrayPool<byte>.Shared.Rent(bytesToRead);
		try {
			var bytesRead = stream.ReadAtLeast(buffer.AsSpan(0, bytesToRead), bytesToRead, throwOnEndOfStream: false);
			var hashBytes = SHA256.HashData(buffer.AsSpan(0, bytesRead));
			return Convert.ToHexString(hashBytes);
		} finally {
			ArrayPool<byte>.Shared.Return(buffer);
		}
	}

	/// <summary>
	/// ファイル全体からSHA256ハッシュを計算する
	/// </summary>
	/// <param name="filePath">ファイルパス</param>
	/// <returns>ハッシュ文字列（16進数）</returns>
	public static string ComputeFullFileHash(string filePath) {
		using var fs = File.OpenRead(filePath);
		var hashBytes = SHA256.HashData(fs);
		return Convert.ToHexString(hashBytes);
	}
}
