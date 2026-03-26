using MediaDeck.Composition.Enum;

namespace MediaDeck.Composition.Interfaces.Services;

public interface IFilePathService {
	/// <summary>
	/// サムネイル相対ファイルパス取得
	/// </summary>
	/// <param name="filePath">生成元ファイルパス</param>
	/// <returns>サムネイル相対ファイルパス</returns>
	public string GetThumbnailRelativeFilePath(string filePath);

	/// <summary>
	/// サムネイル絶対ファイルパス取得
	/// </summary>
	/// <param name="thumbRelativePath">サムネイル相対ファイルパス</param>
	/// <returns>サムネイル絶対ファイルパス</returns>
	public string GetThumbnailAbsoluteFilePath(string thumbRelativePath);

	/// <summary>
	/// 指定したファイルパスのファイルが管理対象の拡張子を持っているかどうかを調べる
	/// </summary>
	/// <param name="path">ファイルパス</param>
	/// <returns>管理対象か否か</returns>
	public bool IsTargetFile(string path);

	/// <summary>
	/// 指定したファイルパスのファイルが動画拡張子を持っているかどうかを調べる
	/// </summary>
	/// <param name="path">ファイルパス</param>
	/// <returns>動画ファイルか否か</returns>
	public bool IsVideoFile(string path);

	/// <summary>
	/// 指定したファイルパスのファイルが画像拡張子を持っているかどうかを調べる
	/// </summary>
	/// <param name="path">ファイルパス</param>
	/// <returns>画像ファイルか否か</returns>
	public bool IsImageFile(string path);

	/// <summary>
	/// 指定したファイルパスのメディアタイプを取得する
	/// </summary>
	/// <param name="path">ファイルパス</param>
	/// <returns>メディアタイプ</returns>
	public MediaType? GetMediaType(string path);
}
