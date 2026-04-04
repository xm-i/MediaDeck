using System.Diagnostics;

namespace MediaDeck.Common.Utilities;

/// <summary>
/// シェル連携機能を提供するユーティリティクラス
/// </summary>
public static class ShellUtility {
	/// <summary>
	/// Explorerでファイルを選択した状態で表示します
	/// </summary>
	/// <param name="filePath">選択するファイルのフルパス</param>
	public static void ShowInExplorer(string filePath) {
		if (string.IsNullOrEmpty(filePath)) {
			return;
		}

		var psi = new ProcessStartInfo { FileName = "explorer.exe", UseShellExecute = false };
		psi.ArgumentList.Add("/select," + filePath);
		Process.Start(psi);
	}

	/// <summary>
	/// 現在のフォアグラウンドウィンドウと同じモニターでファイルを開きます
	/// </summary>
	/// <param name="filePath">開く対象のファイルパス</param>
	/// <param name="arguments">プログラムに渡す引数。nullの場合はファイル単体として開きます</param>
	public static void ShellExecute(string filePath, string? arguments = null) {
		var psi = new ProcessStartInfo {
			UseShellExecute = false
		};

		if (arguments != null) {
			psi.FileName = filePath;
			psi.Arguments = arguments;
		} else {
			psi.FileName = "explorer.exe";
			psi.ArgumentList.Add(filePath);
		}

		Process.Start(psi);
	}
}
