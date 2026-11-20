namespace MediaDeck.Composition.Constants;

/// <summary>
/// アプリケーションで使用する各種ファイル・ディレクトリパスの定数および解決ロジックを提供します。
/// </summary>
public static class FilePathConstants {
	private static readonly string DefaultBaseDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MediaDeck");
	private static string? _baseDirectory;

	/// <summary>
	/// アプリケーションデータ保存先のベースディレクトリを取得します。
	/// </summary>
	public static string BaseDirectory {
		get {
			return _baseDirectory ?? DefaultBaseDirectory;
		}
	}

	/// <summary>
	/// サムネイル生成失敗時に表示する代替画像ファイルパスを取得します。
	/// </summary>
	public static string NoThumbnailFilePath {
		get;
	} = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "thumbnail_creation_failed.png");

	/// <summary>
	/// 状態ファイルのパスを取得します。
	/// </summary>
	public static string StateFilePath {
		get {
			return Path.Combine(BaseDirectory, "MediaDeck.states");
		}
	}

	/// <summary>
	/// 設定ファイルのパスを取得します。
	/// </summary>
	public static string ConfigFilePath {
		get {
			return Path.Combine(BaseDirectory, "MediaDeck.config");
		}
	}

	/// <summary>
	/// サムネイル保存先ディレクトリのパスを取得します。
	/// </summary>
	public static string ThumbnailDirectoryPath {
		get {
			return Path.Combine(BaseDirectory, "thumbs");
		}
	}

	/// <summary>
	/// データベースファイルのパスを取得します。
	/// </summary>
	public static string DatabaseFilePath {
		get {
			return Path.Combine(BaseDirectory, "pix.db");
		}
	}

	/// <summary>
	/// アプリケーションデータ保存先のベースディレクトリを上書き設定します。
	/// </summary>
	/// <param name="baseDirectory">上書きするベースディレクトリパス。</param>
	public static void OverrideBaseDirectory(string? baseDirectory) {
		if (string.IsNullOrWhiteSpace(baseDirectory)) {
			throw new ArgumentException("Base directory must not be empty or whitespace.", nameof(baseDirectory));
		}

		_baseDirectory = baseDirectory;
	}
}