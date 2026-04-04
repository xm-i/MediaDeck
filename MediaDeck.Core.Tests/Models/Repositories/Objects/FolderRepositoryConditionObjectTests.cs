using System;
using System.IO;
using Shouldly;
using MediaDeck.Core.Models.Repositories.Objects;
using MediaDeck.Database.Tables;
using Xunit;

namespace MediaDeck.Core.Tests.Models.Repositories.Objects;

/// <summary>
///     <see cref="FolderRepositoryConditionObject" />のテストクラスです。
/// </summary>
public class FolderRepositoryConditionObjectTests {
	/// <summary>
	///     DirectoryPathがnullの場合、WherePredicateがInvalidOperationExceptionをスローすることを確認します。
	/// </summary>
	[Fact]
	public void WherePredicate_DirectoryPathIsNull_ThrowsInvalidOperationException() {
		// Arrange
		var obj = new FolderRepositoryConditionObject {
			DirectoryPath = null!,
			IncludeSubDirectories = false
		};

		// Act
		var act = () => obj.WherePredicate();

		// Assert
		Should.Throw<InvalidOperationException>(act);
	}

	/// <summary>
	///     IncludeSubDirectoriesがfalseの場合、完全一致の条件式が返されることを確認します。
	/// </summary>
	[Fact]
	public void WherePredicate_IncludeSubDirectoriesIsFalse_ReturnsExactMatchExpression() {
		// Arrange
		var obj = new FolderRepositoryConditionObject {
			DirectoryPath = "/test/dir",
			IncludeSubDirectories = false
		};

		// Act
		var predicate = obj.WherePredicate().Compile();

		// Assert
		predicate(new MediaFile { DirectoryPath = "/test/dir", FilePath = "test1.jpg", Description = "desc" }).ShouldBeTrue();
		predicate(new MediaFile { DirectoryPath = "/test/dir/sub", FilePath = "test2.jpg", Description = "desc" }).ShouldBeFalse();
		predicate(new MediaFile { DirectoryPath = "/other/dir", FilePath = "test3.jpg", Description = "desc" }).ShouldBeFalse();
	}

	/// <summary>
	///     IncludeSubDirectoriesがtrueの場合、前方一致の条件式が返されることを確認します。
	/// </summary>
	[Fact]
	public void WherePredicate_IncludeSubDirectoriesIsTrue_ReturnsStartsWithExpression() {
		// Arrange
		var obj = new FolderRepositoryConditionObject {
			DirectoryPath = "/test/dir",
			IncludeSubDirectories = true
		};

		// Act
		var predicate = obj.WherePredicate().Compile();

		// Assert
		predicate(new MediaFile { DirectoryPath = "/test/dir", FilePath = "test1.jpg", Description = "desc" }).ShouldBeTrue();
		predicate(new MediaFile { DirectoryPath = $"/test/dir{Path.DirectorySeparatorChar}sub", FilePath = "test2.jpg", Description = "desc" }).ShouldBeTrue();
		predicate(new MediaFile { DirectoryPath = $"/test/dir{Path.DirectorySeparatorChar}sub{Path.DirectorySeparatorChar}deep", FilePath = "test3.jpg", Description = "desc" }).ShouldBeTrue();

		// Ensure it doesn't match a sibling directory that just happens to start with the same name
		predicate(new MediaFile { DirectoryPath = "/test/dirt", FilePath = "test4.jpg", Description = "desc" }).ShouldBeFalse();

		predicate(new MediaFile { DirectoryPath = "/other/dir", FilePath = "test5.jpg", Description = "desc" }).ShouldBeFalse();
	}
}
