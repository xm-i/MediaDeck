using MediaDeck.Composition.Interfaces.Services;

namespace MediaDeck.Core.Tests;

/// <summary>
/// テスト用の IAppPathProvider 実装。
/// 一時ディレクトリを使用してパスを提供し、テスト間の相互干渉を防ぎます。
/// </summary>
public class StubAppPathProvider : IAppPathProvider {
	private readonly string _tempBaseDir;

	public StubAppPathProvider() {
		this._tempBaseDir = Path.Combine(Path.GetTempPath(), "MediaDeck_Tests_" + Guid.NewGuid().ToString("N"));
		if (!Directory.Exists(this._tempBaseDir)) {
			Directory.CreateDirectory(this._tempBaseDir);
		}
	}

	public string BaseDirectory {
		get {
			return this._tempBaseDir;
		}
	}

	public string StateFilePath {
		get {
			return Path.Combine(this._tempBaseDir, "MediaDeck.states");
		}
	}

	public string ConfigFilePath {
		get {
			return Path.Combine(this._tempBaseDir, "MediaDeck.config");
		}
	}

	public string NoThumbnailFilePath {
		get {
			return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "thumbnail_creation_failed.png");
		}
	}

	/// <summary>
	/// テスト終了時に一時ディレクトリを削除します。
	/// </summary>
	public void Cleanup() {
		if (Directory.Exists(this._tempBaseDir)) {
			try {
				Directory.Delete(this._tempBaseDir, true);
			} catch {
				// 削除失敗は無視（OSによるファイルロック等）
			}
		}
	}
}