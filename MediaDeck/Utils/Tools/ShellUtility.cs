using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MediaDeck.Utils.Tools;

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
	/// 現在のフォアグラウンドウィンドウと同じモニターでファイルを開く
	/// </summary>
	public static void ShellExecute(string filePath, string? arguments = null) {
		var hwnd = GetForegroundWindow();

		var info = new SHELLEXECUTEINFOW {
			cbSize = Marshal.SizeOf<SHELLEXECUTEINFOW>(),
			hwnd = hwnd,
			lpVerb = "open",
			lpFile = filePath,
			lpParameters = arguments,
			nShow = SW_SHOWNORMAL
		};

		if (!ShellExecuteExW(ref info)) {
			// フォールバック: 通常のProcess.Startを使用
			var psi = new ProcessStartInfo {
				FileName = filePath,
				Arguments = arguments ?? "",
				UseShellExecute = true
			};
			Process.Start(psi);
		}
	}
}
