namespace MediaDeck.Core.Services.FileChangeMonitor;

/// <summary>
/// ファイル変更監視におけるパス正規化および階層判定を行う共通ユーティリティです。
/// </summary>
internal static class FileChangePathUtility {
	/// <summary>
	/// 末尾のディレクトリ区切り文字を除去してパスを正規化します。
	/// </summary>
	/// <param name="path">対象パス</param>
	/// <returns>正規化されたパス</returns>
	public static string NormalizePath(string path) {
		if (string.IsNullOrWhiteSpace(path)) {
			return string.Empty;
		}
		return Path.TrimEndingDirectorySeparator(path);
	}

	/// <summary>
	/// 指定したパスが親パスの配下（子孫パス）であるかを判定します。
	/// </summary>
	/// <param name="candidatePath">判定対象のパス</param>
	/// <param name="parentPath">親パス</param>
	/// <returns>子孫パスである場合は true</returns>
	public static bool IsSubPathOf(string candidatePath, string parentPath) {
		if (string.IsNullOrEmpty(candidatePath) || string.IsNullOrEmpty(parentPath)) {
			return false;
		}
		if (parentPath.EndsWith('\\') || parentPath.EndsWith('/')) {
			return candidatePath.Length > parentPath.Length
				&& candidatePath.StartsWith(parentPath, StringComparison.OrdinalIgnoreCase);
		}
		return candidatePath.Length > parentPath.Length
			&& candidatePath.StartsWith(parentPath, StringComparison.OrdinalIgnoreCase)
			&& (candidatePath[parentPath.Length] == '\\' || candidatePath[parentPath.Length] == '/');
	}

	/// <summary>
	/// 対象パスが基準パスと一致する、またはその配下にあるかを判定します。
	/// </summary>
	/// <param name="candidatePath">判定対象のパス</param>
	/// <param name="basePath">基準パス</param>
	/// <returns>一致または配下にある場合は true</returns>
	public static bool IsSelfOrDescendant(string candidatePath, string basePath) {
		var normCandidate = NormalizePath(candidatePath);
		var normBase = NormalizePath(basePath);
		if (string.IsNullOrEmpty(normCandidate) || string.IsNullOrEmpty(normBase)) {
			return false;
		}
		return string.Equals(normCandidate, normBase, StringComparison.OrdinalIgnoreCase)
			|| IsSubPathOf(normCandidate, normBase);
	}

	/// <summary>
	/// パスの先頭にある旧プレフィックスを新プレフィックスへ置換します。
	/// </summary>
	/// <param name="path">置換対象のパス</param>
	/// <param name="oldPrefix">旧プレフィックス</param>
	/// <param name="newPrefix">新プレフィックス</param>
	/// <returns>置換後のパス</returns>
	public static string ReplacePathPrefix(string path, string oldPrefix, string newPrefix) {
		var oldHasTrailingSep = oldPrefix.EndsWith('\\') || oldPrefix.EndsWith('/');
		var newHasTrailingSep = newPrefix.EndsWith('\\') || newPrefix.EndsWith('/');
		var suffix = path[oldPrefix.Length..];

		if (oldHasTrailingSep && !newHasTrailingSep) {
			return newPrefix + Path.DirectorySeparatorChar + suffix;
		}
		if (!oldHasTrailingSep && newHasTrailingSep) {
			return newPrefix + suffix.TrimStart('\\', '/');
		}
		return newPrefix + suffix;
	}

	/// <summary>
	/// プレフィックス検索用にバックスラッシュおよびスラッシュ終端の接頭辞パターンを生成します。
	/// </summary>
	/// <param name="prefix">基準ディレクトリプレフィックス</param>
	/// <returns>バックスラッシュ終端・スラッシュ終端の各接頭辞</returns>
	public static (string BackslashPrefix, string SlashPrefix) CreatePrefixPatterns(string prefix) {
		var hasTrailingSep = prefix.EndsWith('\\') || prefix.EndsWith('/');
		var sepBack = hasTrailingSep ? prefix : prefix + "\\";
		var sepFwd = hasTrailingSep ? prefix : prefix + "/";
		return (sepBack, sepFwd);
	}
}