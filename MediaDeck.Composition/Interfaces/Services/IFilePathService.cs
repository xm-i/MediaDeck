namespace MediaDeck.Composition.Interfaces.Services;

public interface IFilePathService {
	/// <summary>
	/// サムネイル相対ファイルパス取得
	/// </summary>
	/// <returns>サムネイル相対ファイルパス</returns>
	public string GetThumbnailRelativeFilePath();

	/// <summary>
	/// サムネイル絶対ファイルパス取得
	/// </summary>
	/// <param name="thumbRelativePath">サムネイル相対ファイルパス</param>
	/// <returns>サムネイル絶対ファイルパス</returns>
	public string GetThumbnailAbsoluteFilePath(string thumbRelativePath);

}