using MediaDeck.Composition.Constants;
using MediaDeck.Core.Services;
using Shouldly;

namespace MediaDeck.Core.Tests.Services;

/// <summary>
///     <see cref="AppPathProvider"/> のテストクラスです。
/// </summary>
public class AppPathProviderTests {
	[Fact]
	public void BaseDirectory_ReturnsFilePathConstant() {
		var provider = new AppPathProvider();
		provider.BaseDirectory.ShouldBe(FilePathConstants.BaseDirectory);
	}

	[Fact]
	public void StateFilePath_ReturnsFilePathConstant() {
		var provider = new AppPathProvider();
		provider.StateFilePath.ShouldBe(FilePathConstants.StateFilePath);
	}

	[Fact]
	public void ConfigFilePath_ReturnsFilePathConstant() {
		var provider = new AppPathProvider();
		provider.ConfigFilePath.ShouldBe(FilePathConstants.ConfigFilePath);
	}

	[Fact]
	public void NoThumbnailFilePath_ReturnsFilePathConstant() {
		var provider = new AppPathProvider();
		provider.NoThumbnailFilePath.ShouldBe(FilePathConstants.NoThumbnailFilePath);
	}

	/// <summary>
	/// <see cref="FilePathConstants.DatabaseFilePath"/> および <see cref="FilePathConstants.ThumbnailDirectoryPath"/> が
	/// <see cref="FilePathConstants.BaseDirectory"/> 配下の期待パスを返すことを検証します。
	/// </summary>
	[Fact]
	public void DerivedPaths_AreUnderBaseDirectory() {
		FilePathConstants.DatabaseFilePath.ShouldBe(Path.Combine(FilePathConstants.BaseDirectory, "pix.db"));
		FilePathConstants.ThumbnailDirectoryPath.ShouldBe(Path.Combine(FilePathConstants.BaseDirectory, "thumbs"));
	}
}