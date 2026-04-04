using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace MediaDeck.Common.Utilities;

/// <summary>
/// シェル連携機能を提供するユーティリティクラス
/// </summary>
public static class ShellUtility {
	[DllImport("user32.dll")]
	private static extern nint GetForegroundWindow();

	[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool ShellExecuteExW(ref SHELLEXECUTEINFOW lpExecInfo);

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	private struct SHELLEXECUTEINFOW {
		public int cbSize;
		public uint fMask;
		public nint hwnd;

		[MarshalAs(UnmanagedType.LPWStr)]
		public string lpVerb;

		[MarshalAs(UnmanagedType.LPWStr)]
		public string lpFile;

		[MarshalAs(UnmanagedType.LPWStr)]
		public string? lpParameters;

		[MarshalAs(UnmanagedType.LPWStr)]
		public string? lpDirectory;

		public int nShow;
		public nint hInstApp;
		public nint lpIDList;

		[MarshalAs(UnmanagedType.LPWStr)]
		public string? lpClass;

		public nint hkeyClass;
		public uint dwHotKey;
		public nint hIcon;
		public nint hProcess;
	}

	private const int SW_SHOWNORMAL = 1;

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
		if (string.IsNullOrEmpty(filePath)) return;

		// セキュリティ対策:
		// PATH上の任意の実行ファイル（cmd.exe等）やURLプロトコルの意図しない起動を防ぐため絶対パス化する
		var fullPath = Path.GetFullPath(filePath);

		var hwnd = GetForegroundWindow();

		var info = new SHELLEXECUTEINFOW {
			cbSize = Marshal.SizeOf<SHELLEXECUTEINFOW>(),
			hwnd = hwnd,
			lpVerb = "open",
			lpFile = fullPath,
			lpParameters = arguments,
			nShow = SW_SHOWNORMAL
		};

		if (!ShellExecuteExW(ref info)) {
			// フォールバック: セキュアなProcess.Startを使用
			var psi = new ProcessStartInfo {
				UseShellExecute = false
			};

			if (arguments != null) {
				psi.FileName = fullPath;
				psi.Arguments = arguments;
			} else {
				psi.FileName = "explorer.exe";
				psi.ArgumentList.Add(fullPath);
			}

			Process.Start(psi);
		}
	}
}
