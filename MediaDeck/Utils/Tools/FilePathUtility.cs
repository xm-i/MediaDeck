using System.Text;
using System.Security.Cryptography;
using System.IO;
using CommunityToolkit.Mvvm.DependencyInjection;
using MediaDeck.Utils.Enums;
using MediaDeck.Models.Preferences;

namespace MediaDeck.Utils.Tools;
internal static class FilePathUtility {
	private static readonly Config _config;
	static FilePathUtility() {
		_config = Ioc.Default.GetRequiredService<Config>();
	}
    /// <summary>
    /// サムネイル相対ファイルパス取得
    /// </summary>
    /// <param name="filePath">生成元ファイルパス</param>
    /// <returns>サムネイル相対ファイルパス</returns>
    public static string GetThumbnailRelativeFilePath(string filePath) {
		return $"{string.Join("", SHA512.HashData(Encoding.UTF8.GetBytes(filePath)).Select(b => $"{b:X2}")).Insert(2, @"\")}.jpg";
	}

	/// <summary>
	/// サムネイル絶対ファイルパス取得
	/// </summary>
	/// <param name="filePath">サムネイル相対ファイルパス</param>
	/// <returns>サムネイル絶対ファイルパス</returns>
	public static string GetThumbnailAbsoluteFilePath(string thumbRelativePath) {
		return Path.Combine(_config.PathConfig.ThumbnailFolderPath.Value, thumbRelativePath);
	}

	/// <summary>
	/// 指定したファイルパスのファイルが管理対象の拡張子を持っているかどうかを調べる
	/// </summary>
	/// <param name="path">ファイルパス</param>
	/// <returns>管理対象か否か</returns>
	public static bool IsTargetFile(this string path) {
		return _config.ScanConfig.TargetExtensions.Select(x => x.Extension.Value.Equals(Path.GetExtension(path), StringComparison.CurrentCultureIgnoreCase)).Any();
	}

	/// <summary>
	/// 指定したファイルパスのファイルが動画拡張子を持っているかどうかを調べる
	/// </summary>
	/// <param name="path">ファイルパス</param>
	/// <returns>動画ファイルか否か</returns>
	public static bool IsVideoFile(this string path) {
		return _config.ScanConfig.TargetExtensions.Where(x => x.MediaType.Value == MediaType.Video).Select(x => x.Extension.Value.Equals(Path.GetExtension(path),StringComparison.CurrentCultureIgnoreCase)).Any();
	}

	/// <summary>
	/// 指定したファイルパスのファイルが画像拡張子を持っているかどうかを調べる
	/// </summary>
	/// <param name="path">ファイルパス</param>
	/// <returns>画像ファイルか否か</returns>
	public static bool IsImageFile(this string path) {
		return _config.ScanConfig.TargetExtensions.Where(x => x.MediaType.Value == MediaType.Image).Select(x => x.Extension.Value.Equals(Path.GetExtension(path), StringComparison.CurrentCultureIgnoreCase)).Any();
	}

	/// <summary>
	/// 指定したファイルパスのメディアタイプを取得する
	/// </summary>
	/// <param name="path">ファイルパス</param>
	/// <returns>メディアタイプ</returns>
	public static MediaType? GetMediaType(this string path) {
		return _config.ScanConfig.TargetExtensions.Where(x => x.Extension.Value.Equals(Path.GetExtension(path), StringComparison.CurrentCultureIgnoreCase)).Select(x => x.MediaType.Value as MediaType?).FirstOrDefault();
	}
}
