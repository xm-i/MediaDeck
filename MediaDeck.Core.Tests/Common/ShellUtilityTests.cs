using MediaDeck.Common.Utilities;
using Shouldly;

namespace MediaDeck.Core.Tests.Common;

/// <summary>
/// <see cref="ShellUtility"/> の単体テスト。
/// </summary>
public class ShellUtilityTests {
	/// <summary>
	/// <see cref="ShellUtility.CreateShowInExplorerStartInfo(string)"/> が Windows フォルダ配下の絶対パスで
	/// explorer.exe を指定し、対象ファイルを /select 引数に設定することを検証します。
	/// </summary>
	[Fact]
	public void CreateShowInExplorerStartInfo_ShouldUseAbsoluteExplorerPathAndSelectArgument() {
		var samplePath = Path.Combine("C:", "temp", "sample.jpg");
		var expectedExplorerPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe");

		var psi = ShellUtility.CreateShowInExplorerStartInfo(samplePath);

		if (OperatingSystem.IsWindows()) {
			Path.IsPathRooted(psi.FileName).ShouldBeTrue();
		}
		psi.FileName.ShouldBe(expectedExplorerPath);
		psi.Arguments.ShouldBe($"/select,\"{samplePath}\"");
		psi.UseShellExecute.ShouldBeFalse();
	}

	/// <summary>
	/// ファイルパスが null または空文字の場合に <see cref="ShellUtility.ShowInExplorer(string)"/> が
	/// 例外を投げずに処理を終えることを検証します。
	/// </summary>
	[Fact]
	public void ShowInExplorer_WhenFilePathIsNullOrEmpty_ShouldNotThrow() {
		Should.NotThrow(() => ShellUtility.ShowInExplorer(string.Empty));
		Should.NotThrow(() => ShellUtility.ShowInExplorer(null!));
	}
}