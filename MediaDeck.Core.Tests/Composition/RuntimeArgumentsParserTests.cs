using MediaDeck.Composition.Startup;
using Shouldly;

namespace MediaDeck.Core.Tests.Composition;

/// <summary>
/// <see cref="RuntimeArgumentsParser"/> のコマンドライン引数解析を検証するテストクラスです。
/// </summary>
public sealed class RuntimeArgumentsParserTests {
	/// <summary>
	/// スラッシュ形式（/base:&lt;path&gt;）で指定されたベースディレクトリがトリム・正規化されて設定されることを検証します。
	/// </summary>
	[Fact]
	public void Parse_WithSlashColonFormat_SetsNormalizedBaseDirectory() {
		var targetPath = Path.Combine(Path.GetTempPath(), "mediadeck-slash-base");

		var options = RuntimeArgumentsParser.Parse([$"/Base:  {targetPath}  "]);

		options.BaseDirectory.ShouldBe(Path.GetFullPath(targetPath));
	}

	/// <summary>
	/// ダブルハイフン等号形式（--base=&lt;path&gt;）で指定されたベースディレクトリが正規化されて設定されることを検証します。
	/// </summary>
	[Fact]
	public void Parse_WithDoubleDashEqualsFormat_SetsNormalizedBaseDirectory() {
		var relativeSegment = Path.Combine("relative-base", "sub", "..", "final");

		var options = RuntimeArgumentsParser.Parse([$"--base={relativeSegment}"]);

		options.BaseDirectory.ShouldBe(Path.GetFullPath(relativeSegment));
	}

	/// <summary>
	/// ダブルハイフン空白区切り形式（--base &lt;path&gt;）で次の引数が消費され、ベースディレクトリが設定されることを検証します。
	/// </summary>
	[Fact]
	public void Parse_WithDoubleDashSeparateArg_ConsumesNextArgAndSetsBaseDirectory() {
		var targetPath = Path.Combine(Path.GetTempPath(), "mediadeck-separate-base");

		var options = RuntimeArgumentsParser.Parse(["--base", $"  {targetPath}  ", "--unknown=ignored"]);

		options.BaseDirectory.ShouldBe(Path.GetFullPath(targetPath));
	}

	/// <summary>
	/// 複数回ベースディレクトリが指定された場合に大文字小文字を区別せず後の指定が優先されることを検証します。
	/// </summary>
	[Fact]
	public void Parse_WhenBaseSpecifiedMultipleTimes_UsesLastValueCaseInsensitively() {
		var firstPath = Path.Combine(Path.GetTempPath(), "mediadeck-first");
		var secondPath = Path.Combine(Path.GetTempPath(), "mediadeck-second");

		var options = RuntimeArgumentsParser.Parse(["--BASE", firstPath, $"/base:{secondPath}"]);

		options.BaseDirectory.ShouldBe(Path.GetFullPath(secondPath));
	}

	/// <summary>
	/// 未知のキーや不正な形式・空値の引数ではベースディレクトリが設定されないことを検証します。
	/// </summary>
	/// <param name="args">検証対象のコマンドライン引数配列。</param>
	[Theory]
	[MemberData(nameof(GetIgnoredArgumentsCases))]
	public void Parse_WithIgnoredOrInvalidArguments_ReturnsNullBaseDirectory(string[] args) {
		var options = RuntimeArgumentsParser.Parse(args);

		options.BaseDirectory.ShouldBeNull();
	}

	/// <summary>
	/// ベースディレクトリとして解釈されない各種引数パターンを返します。
	/// </summary>
	/// <returns>テストケースの引数配列一覧。</returns>
	public static TheoryData<string[]> GetIgnoredArgumentsCases() {
		return new() {
			([]),
			(["plain-argument"]),
			(["--other=value"]),
			(["/other:value"]),
			(["/base"]),
			(["/base:"]),
			(["/base:   "]),
			(["/:value"]),
			(["--base="]),
			(["--base=   "]),
			(["--=value"]),
			(["--base"]),
			(["--base", "   "]),
			(["--", Path.GetTempPath()]),
		};
	}
}